#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Lokad.Quality;
using Microsoft.WindowsAzure.StorageClient;

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
		readonly ActionPolicy _azureServerPolicy;

		// messages currently being processed (boolean property indicates if the message is overflowing)
		private readonly Dictionary<object, InProcessMessage> _inProcessMessages;

		/// <summary>IoC constructor.</summary>
		public QueueStorageProvider(
			CloudQueueClient queueStorage, IBlobStorageProvider blobStorage, IBinaryFormatter formatter)
		{
			_queueStorage = queueStorage;
			_blobStorage = blobStorage;
			_formatter = formatter;
			_azureServerPolicy = AzurePolicies.TransientServerErrorBackOff;

			_inProcessMessages = new Dictionary<object, InProcessMessage>(20);
		}

		public IEnumerable<string> List(string prefix)
		{
			foreach (var queue in _queueStorage.ListQueues(prefix))
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
			catch (SerializationException)
			{
				source.Position = position;
				item = _formatter.Deserialize(source, typeof(MessageWrapper));
			}

			return item;
		}

		public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout)
		{
			var queue = _queueStorage.GetQueueReference(queueName);

			IEnumerable<CloudQueueMessage> rawMessages;

			try
			{
				rawMessages = _azureServerPolicy.Get(() => queue.GetMessages(count, visibilityTimeout));
			}
			catch (StorageClientException ex)
			{
				// if the queue does not exist return an empty collection.
				if (ex.ErrorCode == StorageErrorCode.ResourceNotFound
					|| ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
				{
					return new T[0];
				}

				throw;
			}

			// skip empty queue
			if (null == rawMessages)
			{
				return new T[0];
			}

			var messages = new List<T>(count);
			var wrappedMessages = new List<MessageWrapper>();

			lock (_sync)
			{
				foreach (var rawMessage in rawMessages)
				{
					object innerMessage;
					using (var stream = new MemoryStream(rawMessage.AsBytes))
					{
						innerMessage = SafeDeserialize<T>(stream);
					}

					if (innerMessage is T)
					{
						messages.Add((T)innerMessage);

						// If T is a value type, _inprocess could already contain the message
						// (not the same exact instance, but an instance that is value-equal to this one)
						InProcessMessage inProcMsg;
						if (!_inProcessMessages.TryGetValue(innerMessage, out inProcMsg))
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
						var mw = (MessageWrapper)innerMessage;
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
			foreach (var mw in wrappedMessages)
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

					try
					{
						_azureServerPolicy.Do(() => queue.DeleteMessage(rawMessage));
					}
					catch (StorageClientException ex)
					{
						if (ex.ErrorCode == StorageErrorCode.ResourceNotFound
							|| ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
						{
							continue;
						}

						throw;
					}
					
					// skipping the message if it can't be unwrapped
					continue;
				}

				T innerMessage = (T)blobContent.Value;

				// substitution: message wrapper replaced by actual item in '_inprocess' list
				lock (_sync)
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
			PutRange(queueName, new[] { message });
		}

		public void PutRange<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueueReference(queueName);

			foreach (var message in messages)
			{
				using (var stream = new MemoryStream())
				{
					_formatter.Serialize(stream, message);

					// Caution: MaxMessageSize is not related to the number of bytes
					// but the number of characters when Base64-encoded:

					CloudQueueMessage queueMessage;
					if (stream.Length >= (CloudQueueMessage.MaxMessageSize - 1) / 4 * 3)
					{
						queueMessage = new CloudQueueMessage(PutOverflowingMessageAndWrap(queueName, message));
					}
					else
					{
						try
						{
							queueMessage = new CloudQueueMessage(stream.ToArray());
						}
						catch (ArgumentException)
						{
							queueMessage = new CloudQueueMessage(PutOverflowingMessageAndWrap(queueName, message));
						}
					}

					try
					{
						_azureServerPolicy.Do(() => queue.AddMessage(queueMessage));
					}
					catch (StorageClientException ex)
					{
						// HACK: not storage status error code yet
						if (ex.ErrorCode == StorageErrorCode.ResourceNotFound
							|| ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
						{
							// It usually takes time before the queue gets available
							// (the queue might also have been freshly deleted).
							AzurePolicies.SlowInstantiation.Do(() =>
								{
									queue.Create();
									queue.AddMessage(queueMessage);
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

		byte[] PutOverflowingMessageAndWrap<T>(string queueName, T message)
		{
			var blobRef = OverflowingMessageBlobReference<T>.GetNew(queueName);

			// HACK: In this case serialization is performed another time (internally)
			_blobStorage.PutBlob(blobRef, message);

			var mw = new MessageWrapper
			{
				ContainerName = blobRef.ContainerName,
				BlobName = blobRef.ToString()
			};

			using (var stream = new MemoryStream())
			{
				_formatter.Serialize(stream, mw);
				return stream.ToArray();
			}
		}

		void DeleteOverflowingMessages(string queueName)
		{
			foreach (var blobName in _blobStorage.List(OverflowingMessagesContainerName, queueName))
			{
				// TODO: leverage lazy implementation of 'List' to speed-up the process here.
				_blobStorage.DeleteBlob(OverflowingMessagesContainerName, blobName);
			}
		}

		public void Clear(string queueName)
		{
			try
			{
				// caution: call 'DeleteOverflowingMessages' first (BASE).
				DeleteOverflowingMessages(queueName);
				var queue = _queueStorage.GetQueueReference(queueName);
				_azureServerPolicy.Do(queue.Clear);
			}
			catch (StorageClientException ex)
			{
				// if the queue does not exist do nothing
				if (ex.ErrorCode == StorageErrorCode.ResourceNotFound
					|| ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
				{
					return;
				}
				throw;
			}
		}

		public bool Delete<T>(string queueName, T message)
		{
			return DeleteRange(queueName, new[] { message }) > 0;
		}

		public int DeleteRange<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueueReference(queueName);

			int deletionCount = 0;

			foreach (var message in messages)
			{
				CloudQueueMessage rawMessage;
				bool isOverflowing;

				lock (_sync)
				{
					// ignoring message if already deleted
					InProcessMessage inProcMsg;
					if (!_inProcessMessages.TryGetValue(message, out inProcMsg))
					{
						continue;
					}

					rawMessage = inProcMsg.RawMessages[0];
					isOverflowing = inProcMsg.IsOverflowing;
				}

				// deleting the overflowing copy from the blob storage.
				if (isOverflowing)
				{
					using (var stream = new MemoryStream(rawMessage.AsBytes))
					{
						var mw = (MessageWrapper)_formatter.Deserialize(stream, typeof(MessageWrapper));

						_blobStorage.DeleteBlob(mw.ContainerName, mw.BlobName);
					}
				}

				try
				{
					_azureServerPolicy.Do(() => queue.DeleteMessage(rawMessage));
					deletionCount++;
				}
				catch (StorageClientException ex)
				{
					if (ex.ErrorCode != StorageErrorCode.ResourceNotFound
						&& ex.ExtendedErrorInformation.ErrorCode != QueueErrorCodeStrings.QueueNotFound)
					{
						throw;
					}
				}

				lock (_sync)
				{
					var inProcMsg = _inProcessMessages[message];
					inProcMsg.RawMessages.RemoveAt(0);

					if (0 == inProcMsg.RawMessages.Count)
					{
						_inProcessMessages.Remove(message);
					}
				}
			}

			return deletionCount;
		}


		/// <summary>
		/// Deletes a queue.
		/// </summary>
		/// <returns><c>true</c> if the queue name has been actually deleted.</returns>
		/// <remarks>
		/// This implementation takes care of deleting overflowing blobs as
		/// well.
		/// </remarks>
		public bool DeleteQueue(string queueName)
		{
			try
			{
				// Caution: call to 'DeleteOverflowingMessages' comes first (BASE).
				DeleteOverflowingMessages(queueName);
				var queue = _queueStorage.GetQueueReference(queueName);
				_azureServerPolicy.Do(queue.Delete);
				return true;
			}
			catch (StorageClientException ex)
			{
				if (ex.ErrorCode == StorageErrorCode.ResourceNotFound
					|| ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
				{
					return false;
				}

				throw;
			}
		}

		/// <summary>
		/// Gets the approximate number of items in this queue.
		/// </summary>
		public int GetApproximateCount(string queueName)
		{
			try
			{
				var queue = _queueStorage.GetQueueReference(queueName);
				return _azureServerPolicy.Get<int>(queue.RetrieveApproximateMessageCount);
			}
			catch (StorageClientException ex)
			{
				// if the queue does not exist, return 0 (no queue)
				if (ex.ErrorCode == StorageErrorCode.ResourceNotFound
					|| ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
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

	public class OverflowingMessageBlobReference<T> : BlobReference<T>
	{
		public override string ContainerName
		{
			get { return QueueStorageProvider.OverflowingMessagesContainerName; }
		}

		[UsedImplicitly, Rank(0)]
		public string QueueName;

		[UsedImplicitly, Rank(1)]
		public Guid MessageId;

		OverflowingMessageBlobReference(string queueName, Guid guid)
		{
			QueueName = queueName;
			MessageId = guid;
		}

		public static OverflowingMessageBlobReference<T> GetNew(string queueName)
		{
			return new OverflowingMessageBlobReference<T>(queueName, Guid.NewGuid());
		}
	}
}
