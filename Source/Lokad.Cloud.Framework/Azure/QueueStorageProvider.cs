#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.WindowsAzure.StorageClient;
using Lokad.Quality;
using Lokad.Threading;

namespace Lokad.Cloud.Azure
{
	/// <summary>Provides access to the Queue Storage (plus the Blob Storage when
	/// messages are overflowing).</summary>
	/// <remarks>
	/// <para>
	/// Overflowing messages are stored in blob storage and normally deleted as with
	/// their originating correspondence in queue storage.
	/// </para>
	/// <para>All the methods of <see cref="QueueStorageProvider"/> are thread-safe.</para>
	/// </remarks>
	public class QueueStorageProvider : IQueueStorageProvider
	{
		internal const string OverflowingMessagesContainerName = "lokad-cloud-overflowing-messages";

		/// <summary>Root used to synchronize accesses to <c>_inprocess</c>. 
		/// Caution: do not hold the lock while performing operations on the cloud
		/// storage.</summary>
		readonly object _sync = new object();

		readonly CloudQueueClient _queueStorage;
		readonly IBlobStorageProvider _blobStorage;
		readonly IBinaryFormatter _formatter;

		// messages currently being processed (boolean property indicates if the message is overflowing)
		private readonly Dictionary<object, InProcessMessage> _inProcessMessages;

		/// <summary>IoC constructor.</summary>
		public QueueStorageProvider(
			CloudQueueClient queueStorage, IBlobStorageProvider blobStorage, IBinaryFormatter formatter)
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

		object SafeDeserialize<T>(Stream source)
		{
			var position = source.Position;

			object item;
			try
			{
				item = _formatter.Deserialize(source, typeof(T));
			}
			catch(SerializationException)
			{
				source.Position = position;
				item = _formatter.Deserialize(source, typeof(MessageWrapper));
			}

			return item;
		}

		public IEnumerable<T> Get<T>(string queueName, int count)
		{
			var queue = _queueStorage.GetQueueReference(queueName);

			IEnumerable<CloudQueueMessage> rawMessages;

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
					object innerMessage;
					using(var stream = new MemoryStream(rawMessage.AsBytes))
					{
						innerMessage = SafeDeserialize<T>(stream);
					}

					if(innerMessage is T)
					{
						messages.Add((T)innerMessage);

						// If T is a value type, _inprocess could already contain the message
						// (not the same exact instance, but an instance that is value-equal to this one)
						InProcessMessage inProcMsg;
						if(!_inProcessMessages.TryGetValue(innerMessage, out inProcMsg))
						{
							inProcMsg = new InProcessMessage
							{
								RawMessages = new List<CloudQueueMessage> { rawMessage },
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

						var overflowingInProcMsg = new InProcessMessage
							{
							RawMessages = new List<CloudQueueMessage> { rawMessage },
							IsOverflowing = true
						};
						_inProcessMessages.Add(mw, overflowingInProcMsg);
					}
				}
			}
			
			// unwrapping messages
            foreach(var mw in wrappedMessages)
            {
				string ignored;
				var blobContent = _blobStorage.GetBlob(mw.ContainerName, mw.BlobName, typeof(T), out ignored);
            	
				// blob may not exists in (rare) case of failure just before queue deletion
				// but after container deletion (or also timeout deletion).
				if (!blobContent.HasValue)
				{
					CloudQueueMessage rawMessage;
					lock (_sync)
					{
						rawMessage = _inProcessMessages[mw].RawMessages[0];
						_inProcessMessages.Remove(mw);
					}

					queue.DeleteMessage(rawMessage);

					// skipping the message if it can't be unwrapped
					continue;
				}

            	T innerMessage = (T) blobContent.Value;

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
			var queue = _queueStorage.GetQueueReference(queueName);

			foreach(var message in messages)
			{
				using(var stream = new MemoryStream())
				{
					_formatter.Serialize(stream, message);

					byte[] messageContent;

					if(stream.Length >= CloudQueueMessage.MaxMessageSize)
					{
						var blobName = OverflowingMessageBlobName.GetNew(queueName);

						// HACK: In this case serialization is performed another time (internally)
						_blobStorage.PutBlob(blobName, message);

						var mw = new MessageWrapper
							{
								ContainerName = blobName.ContainerName,
								BlobName = blobName.ToString()
							};

						using(var otherStream = new MemoryStream())
						{
							_formatter.Serialize(otherStream, mw);
							// buffer gets replaced by the wrapper
							messageContent = otherStream.ToArray();
						}
					}
					else
					{
						messageContent = stream.ToArray();
					}

					try
					{
						queue.AddMessage(new CloudQueueMessage(messageContent));
					}
					catch(StorageClientException ex)
					{
						// HACK: not storage status error code yet
						if(ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
						{
							// It usually takes time before the queue gets available
							// (the queue might also have been freshly deleted).
							PolicyHelper.SlowInstantiation.Do(() =>
								{
									queue.Create();
									queue.AddMessage(new CloudQueueMessage(messageContent));
								});
						}
						else
						{
							throw;
						}
					}
				}
			}
		}

		void DeleteOverflowingMessages(string queueName)
		{
			var toDelete = new List<string>();

			foreach(var blob in _blobStorage.List(OverflowingMessagesContainerName, queueName))
			{
				toDelete.Add(blob);
			}

			toDelete.ToArray().SelectInParallel(blobName =>
			{
				_blobStorage.DeleteBlob(OverflowingMessagesContainerName, blobName);
				return 0;
			});
		}

		public void Clear(string queueName)
		{
			try
			{
				_queueStorage.GetQueueReference(queueName).Clear();
				DeleteOverflowingMessages(queueName);
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
			var queue = _queueStorage.GetQueueReference(queueName);

			int deletionCount = 0;

			foreach(var message in messages)
			{
				CloudQueueMessage rawMessage;
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
					using(var stream = new MemoryStream(rawMessage.AsBytes))
					{
						var mw = (MessageWrapper)_formatter.Deserialize(stream, typeof(MessageWrapper));

						_blobStorage.DeleteBlob(mw.ContainerName, mw.BlobName);
					}
				}

				queue.DeleteMessage(rawMessage);
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
			try
			{
				_queueStorage.GetQueueReference(queueName).Delete();
				DeleteOverflowingMessages(queueName);
				return true;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ResourceNotFound) return false;
				throw;
			}
		}

		public int GetApproximateCount(string queueName)
		{
			try
			{
				return _queueStorage.GetQueueReference(queueName).RetrieveApproximateMessageCount();
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
		/// <summary>The multiple, different raw <see cref="CloudQueueMessage" /> objects as returned from the queue storage.</summary>
		public List<CloudQueueMessage> RawMessages { get; set; }

		/// <summary>A flag indicating whether the original message was bigger than the max allowed size and was
		/// therefore wrapped in <see cref="MessageWrapper" />.</summary>
		public bool IsOverflowing { get; set; }
	}

	public class OverflowingMessageBlobName : BaseBlobName
	{
		public override string ContainerName
		{
			get { return QueueStorageProvider.OverflowingMessagesContainerName; }
		}

		[UsedImplicitly, Rank(0)]
		public string QueueName;

		[UsedImplicitly, Rank(1)]
		public Guid MessageId;

		OverflowingMessageBlobName(string queueName, Guid guid)
		{
			QueueName = queueName;
			MessageId = guid;
		}

		public static OverflowingMessageBlobName GetNew(string queueName)
		{
			return new OverflowingMessageBlobName(queueName, Guid.NewGuid());
		}
	}

}
