#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cloud.Framework;
using Microsoft.Samples.ServiceHosting.StorageClient;

using QueueService = Lokad.Cloud.Framework.QueueService<object>;

namespace Lokad.Cloud.Core
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
		readonly ActionPolicy _policy;
		readonly IFormatter _formatter;

		// messages currently being processed (boolean property indicates if the message is overflowing)
		private readonly Dictionary<object, Tuple<Message, bool>> _inprocess;

		/// <summary>IoC constructor.</summary>
		public QueueStorageProvider(
			QueueStorage queueStorage, BlobStorage blobStorage, ActionPolicy policy, IFormatter formatter)
		{
			_queueStorage = queueStorage;
			_blobStorage = blobStorage;
			_policy = policy;
			_formatter = formatter;

			_inprocess = new Dictionary<object, Tuple<Message, bool>>();
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
						_inprocess.Add(innerMessage, new Tuple<Message, bool>(rawMessage, false));
					}
					else
					{
						// we don't retrieve messages while holding the lock
						var mw = (MessageWrapper) innerMessage;
						wrappedMessages.Add(mw);

						_inprocess.Add(mw, new Tuple<Message, bool>(rawMessage, true));
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
						rawMessage = _inprocess[mw].Item1;
						_inprocess.Remove(mw);
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
					var rawMessage = _inprocess[mw];
					_inprocess.Remove(mw);
					_inprocess.Add(innerMessage, rawMessage);
				}

				messages.Add(innerMessage);
            }

			return messages;
		}

		public void Put<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueue(queueName);

			foreach(var message in messages)
			{
				var stream = new MemoryStream();
				_formatter.Serialize(stream, message);

				var buffer = stream.GetBuffer();

				if(buffer.Length >= Message.MaxMessageSize)
				{
					var container = _blobStorage.GetBlobContainer(QueueService.OverflowingContainer);
					var blobName = GetNewBlobName(queueName);

					var blobContents = new BlobContents(buffer);
					var blobProperties = new BlobProperties(blobName);

					try
					{
						container.CreateBlob(blobProperties, blobContents, false);
					}
					catch (StorageClientException ex)
					{
						if(ex.ErrorCode == StorageErrorCode.ContainerNotFound)
						{
							container.CreateContainer();

							// It usually takes time before the container gets available
							_policy.Do(() => container.CreateBlob(blobProperties, blobContents, false));
						}
						else
						{
							throw;
						}
					}

					var mw = new MessageWrapper
						{
							ContainerName = QueueService.OverflowingContainer, 
							BlobName = blobName
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
						queue.CreateQueue();

						// It usually takes time before the queue gets available
						_policy.Do(() => queue.PutMessage(new Message(buffer)));
					}
					else
					{
						throw;
					}
				}
			}
		}

		public int Delete<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueue(queueName);

			int deletionCount = 0;

			foreach(var message in messages)
			{
				Message rawMessage;
				bool isOverflowing;
 
				lock(_sync)
				{
					var tuple = _inprocess[message];
					rawMessage = tuple.Item1;
					isOverflowing = tuple.Item2;
				}

				// deleting the overflowing copy from the blob storage.
				if(isOverflowing)
				{
					var stream = new MemoryStream(rawMessage.ContentAsBytes());
					var mw = (MessageWrapper)_formatter.Deserialize(stream);

					var container = _blobStorage.GetBlobContainer(mw.ContainerName);
					container.DeleteBlob(mw.BlobName);
				}

				if(queue.DeleteMessage(rawMessage)) deletionCount++;

				lock(_sync)
				{
					_inprocess.Remove(message);
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

		/// <summary>
		/// Naming is following a date pattern to facilitate cleaning later on.
		/// </summary>
		/// <remarks>
		/// The date specified by the blob name prefix correspond to the expiration
		/// date of the overflowing message.
		/// </remarks>
		string GetNewBlobName(string queueName)
		{
			// HACK: [vermorel] the message life-time is hard-coded to 7 days for now.
			// In the future, we might consider a more modular approach where lifetime
			// can be adjusted depending on the queue settings.
			return DateTime.Now.ToUniversalTime().AddDays(7)
				.ToString("yyyy/MM/dd/hh/mm/ss/", CultureInfo.InvariantCulture) 
				+ queueName + "/"
				+ Guid.NewGuid();
		}
	}
}
