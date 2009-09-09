#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cloud;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Azure
{
	/// <summary>Provides access to the Queue Storage (plus the Blob Storage when
	/// messages are overflowing).</summary>
	/// <remarks>
	/// <para>
	/// Overflowing messages are stored in blob storage and normally deleted as with
	/// their originating correspondance in queue storage. Yet if messages aren't processed
	/// in 7 days, then, they should be removed.
	/// </para>
	/// <para>
	/// The pattern for blobname of overflowing message is:
	/// <c>ExpirationDate / QueuName / GUID</c> 
	/// </para>
	/// <para>All the methods of <see cref="QueueStorageProvider"/> are thread-safe.</para>
	/// </remarks>
	public class QueueStorageProvider : IQueueStorageProvider
	{
		/// <summary>Root used to synchronize accesses to <c>_inprocess</c>. 
		/// Caution: do not hold the lock while performing operations on the cloud
		/// storage.</summary>
		readonly object _sync = new object();

		readonly QueueStorage _queueStorage;
		readonly BlobStorage _blobStorage; // needed for overflowing messages
		readonly IFormatter _formatter;

		// messages currently being processed (boolean property indicates if the message is overflowing)
		private readonly Dictionary<object, InProcessMessage> _inProcessMessages;

		/// <summary>IoC constructor.</summary>
		public QueueStorageProvider(
			QueueStorage queueStorage, BlobStorage blobStorage, IFormatter formatter)
		{
			_queueStorage = queueStorage;
			_blobStorage = blobStorage;
			_formatter = formatter;

			_inProcessMessages = new Dictionary<object, InProcessMessage>(20);
		}

		public IEnumerable<string> List(string prefix)
		{
			foreach(var queue in _queueStorage.ListQueues(prefix))
			{
				yield return queue.Name;
			}
		}

		public IEnumerable<T> Get<T>(string queueName, int count)
		{
			var queue = _queueStorage.GetQueue(queueName);

			IEnumerable<Message> rawMessages;

			try
			{
				rawMessages = queue.GetMessages(count);
			}
			catch (StorageClientException ex)
			{
				// if the queue does not exist return an empty collection.
				if (ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
				{
					return new T[0];
				}

				throw;
			}

			// skip empty queue
			if (null == rawMessages) return new T[0];

			var messages = new List<T>(rawMessages.Count());
			var wrappedMessages = new List<MessageWrapper>();

			lock(_sync)
			{
				foreach(var rawMessage in rawMessages)
				{
					var stream = new MemoryStream(rawMessage.ContentAsBytes());
					var innerMessage = _formatter.Deserialize(stream);

					if(innerMessage is T)
					{
						messages.Add((T)innerMessage);

						// If T is a value type, _inprocess could already contain the message
						// (not the same exact instance, but an instance that is value-equal to this one)
						InProcessMessage inProcMsg;
						if(!_inProcessMessages.TryGetValue(innerMessage, out inProcMsg))
						{
							inProcMsg = new InProcessMessage()
							{
								RawMessages = new List<Message>() { rawMessage },
								IsOverflowing = false
							};
							_inProcessMessages.Add(innerMessage, inProcMsg);
						}
						else
						{
							inProcMsg.RawMessages.Add(rawMessage);
						}
					}
					else
					{
						// we don't retrieve messages while holding the lock
						var mw = (MessageWrapper) innerMessage;
						wrappedMessages.Add(mw);

						var overflowingInProcMsg = new InProcessMessage()
						{
							RawMessages = new List<Message>() { rawMessage },
							IsOverflowing = true
						};
						_inProcessMessages.Add(mw, overflowingInProcMsg);
					}
				}
			}
			
			// unwrapping messages
            foreach(var mw in wrappedMessages)
            {
            	var container = _blobStorage.GetBlobContainer(mw.ContainerName);
            	var stream = new MemoryStream();
            	var blobContents = new BlobContents(stream);
            	var blobProperties = container.GetBlob(mw.BlobName, blobContents, false);

				// blob may not exists in (rare) case of failure just before queue deletion
				// but after container deletion (or also timeout deletion).
				if(null == blobProperties)
				{
					Message rawMessage;
					lock (_sync)
					{
						rawMessage = _inProcessMessages[mw].RawMessages[0];
						_inProcessMessages.Remove(mw);
					}

					queue.DeleteMessage(rawMessage);

					// skipping the message if it can't be unwrapped
					continue;
				}

            	stream.Position = 0;
            	var innerMessage = (T)_formatter.Deserialize(stream);

				// substitution: message wrapper replaced by actual item in '_inprocess' list
				lock(_sync)
				{
					var rawMessage = _inProcessMessages[mw];
					_inProcessMessages.Remove(mw);
					_inProcessMessages.Add(innerMessage, rawMessage);
				}

				messages.Add(innerMessage);
            }

			return messages;
		}

		public void Put<T>(string queueName, T message)
		{
			PutRange(queueName, new[]{message});
		}

		public void PutRange<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueue(queueName);

			foreach(var message in messages)
			{
				var stream = new MemoryStream();
				_formatter.Serialize(stream, message);

				var buffer = stream.GetBuffer();

				if(buffer.Length >= Message.MaxMessageSize)
				{
					
					// 7 days = maximal processing duration for messages in queue
					var blobName = TemporaryBlobName.GetNew(DateTime.UtcNow.AddDays(7), queueName);

					var blobContents = new BlobContents(buffer);
					var blobProperties = new BlobProperties(blobName.ToString());

					var container = _blobStorage.GetBlobContainer(blobName.ContainerName);

					try
					{
						container.CreateBlob(blobProperties, blobContents, false);
					}
					catch (StorageClientException ex)
					{
						if(ex.ErrorCode == StorageErrorCode.ContainerNotFound)
						{
							// It usually takes time before the container gets available.
							// (the container might have been freshly deleted).
							PolicyHelper.SlowInstantiation.Do(() =>
								{
									container.CreateContainer();
									container.CreateBlob(blobProperties, blobContents, false);
								});
							
						}
						else
						{
							throw;
						}
					}

					var mw = new MessageWrapper
						{
							ContainerName = CloudService.TemporaryContainer, 
							BlobName = blobName.ToString()
						};
					stream = new MemoryStream();
					_formatter.Serialize(stream, mw);

					// buffer gets replaced by the wrapper
					buffer = stream.GetBuffer();
				}

				try
				{
					queue.PutMessage(new Message(buffer));
				}
				catch (StorageClientException ex)
				{
					// HACK: not storage status error code yet
					if (ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
					{
						// It usually takes time before the queue gets available
						// (the queue might also have been freshly deleted).
						PolicyHelper.SlowInstantiation.Do(() =>
							{
								queue.CreateQueue();
								queue.PutMessage(new Message(buffer));
							});
					}
					else
					{
						throw;
					}
				}
			}
		}

		public void Clear(string queueName)
		{
			try
			{
				// HACK: not sure what is the return code of 'Clear'
				_queueStorage.GetQueue(queueName).Clear();
			}
			catch (StorageClientException ex)
			{
				// if the queue does not exist do nothing
				if (ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
				{
					return;
				}
				throw;
			}
		}

		public bool Delete<T>(string queueName, T message)
		{
			return DeleteRange(queueName, new[] {message}) > 0;
		}

		public int DeleteRange<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueue(queueName);

			int deletionCount = 0;

			foreach(var message in messages)
			{
				Message rawMessage;
				bool isOverflowing;
 
				lock(_sync)
				{
					// ignoring message if already deleted
					InProcessMessage inProcMsg;
					if(!_inProcessMessages.TryGetValue(message, out inProcMsg)) continue;

					rawMessage = inProcMsg.RawMessages[0];
					isOverflowing = inProcMsg.IsOverflowing;
				}

				// deleting the overflowing copy from the blob storage.
				if(isOverflowing)
				{
					var stream = new MemoryStream(rawMessage.ContentAsBytes());
					var mw = (MessageWrapper)_formatter.Deserialize(stream);

					var container = _blobStorage.GetBlobContainer(mw.ContainerName);
					container.DeleteBlob(mw.BlobName);
				}

				if(queue.DeleteMessage(rawMessage))
					deletionCount++;

				lock(_sync)
				{
					var inProcMsg = _inProcessMessages[message];
					inProcMsg.RawMessages.RemoveAt(0);
					
					if(0 == inProcMsg.RawMessages.Count) _inProcessMessages.Remove(message);
				}
			}

			return deletionCount;
		}

		public bool DeleteQueue(string queueName)
		{
			return _queueStorage.GetQueue(queueName).DeleteQueue();
		}

		public int GetApproximateCount(string queueName)
		{
			try
			{
				return _queueStorage.GetQueue(queueName).ApproximateCount();
			}
			catch (StorageClientException ex)
			{
				// if the queue does not exist, return 0 (no queue)
				if (ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
				{
					return 0;
				}

				throw;
			}
		}
	}

	/// <summary>Represents a set of value-identical messages that are being processed by workers, 
	/// i.e. were hidden from the queue because of calls to Get{T}.</summary>
	internal class InProcessMessage
	{
		/// <summary>The multiple, different raw <see cref="T:Message" /> objects as returned from the queue storage.</summary>
		public List<Message> RawMessages { get; set; }

		/// <summary>A flag indicating whether the original message was bigger than the max allowed size and was
		/// therefore wrapped in <see cref="T:MessageWrapper" />.</summary>
		public bool IsOverflowing { get; set; }
	}

}
