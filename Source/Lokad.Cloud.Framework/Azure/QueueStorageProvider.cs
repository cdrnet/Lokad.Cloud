#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Lokad.Diagnostics;
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
	public class QueueStorageProvider : IQueueStorageProvider, IDisposable
	{
		internal const string OverflowingMessagesContainerName = "lokad-cloud-overflowing-messages";
		internal const string PoisonedMessagePersistenceStoreName = "failing-messages";

		/// <summary>Root used to synchronize accesses to <c>_inprocess</c>. 
		/// Caution: do not hold the lock while performing operations on the cloud
		/// storage.</summary>
		readonly object _sync = new object();

		readonly CloudQueueClient _queueStorage;
		readonly IBlobStorageProvider _blobStorage;
		readonly IBinaryFormatter _formatter;
		readonly IRuntimeFinalizer _runtimeFinalizer;
		readonly ActionPolicy _azureServerPolicy;

		// Instrumentation
		readonly ExecutionCounter _countGetMessage;
		readonly ExecutionCounter _countPutMessage;
		readonly ExecutionCounter _countDeleteMessage;
		readonly ExecutionCounter _countAbandonMessage;
		readonly ExecutionCounter _countPersistMessage;
		readonly ExecutionCounter _countWrapMessage;
		readonly ExecutionCounter _countUnwrapMessage;

		// messages currently being processed (boolean property indicates if the message is overflowing)
		/// <summary>Mapping object --> Queue Message Id. Use to delete messages afterward.</summary>
		readonly Dictionary<object, InProcessMessage> _inProcessMessages;

		/// <summary>IoC constructor.</summary>
		public QueueStorageProvider(
			CloudQueueClient queueStorage, IBlobStorageProvider blobStorage,
			IBinaryFormatter formatter, IRuntimeFinalizer runtimeFinalizer)
		{
			_queueStorage = queueStorage;
			_blobStorage = blobStorage;
			_formatter = formatter;
			_runtimeFinalizer = runtimeFinalizer;

			// self-registration for finalization
			_runtimeFinalizer.Register(this);

			_azureServerPolicy = AzurePolicies.TransientServerErrorBackOff;

			_inProcessMessages = new Dictionary<object, InProcessMessage>(20);

			// Instrumentation
			ExecutionCounters.Default.RegisterRange(new[]
				{
					_countGetMessage = new ExecutionCounter("QueueStorageProvider.Get", 0, 0),
					_countPutMessage = new ExecutionCounter("QueueStorageProvider.PutSingle", 0, 0),
					_countDeleteMessage = new ExecutionCounter("QueueStorageProvider.DeleteSingle", 0, 0),
					_countAbandonMessage = new ExecutionCounter("QueueStorageProvider.AbandonSingle", 0, 0),
					_countPersistMessage = new ExecutionCounter("QueueStorageProvider.PersistSingle", 0, 0),
					_countWrapMessage = new ExecutionCounter("QueueStorageProvider.WrapSingle", 0, 0),
					_countUnwrapMessage = new ExecutionCounter("QueueStorageProvider.UnwrapSingle", 0, 0),
				});
		}

		/// <summary>
		/// Disposing the provider will cause an abandon on all currently messages currently
		/// in-process. At the end of the life-cycle of the provider, normally there is no
		/// message in-process.
		/// </summary>
		public void Dispose()
		{
			AbandonRange(_inProcessMessages.Keys.ToArray());
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

		public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout, int maxProcessingTrials)
		{
			var timestamp = _countGetMessage.Open();

			var queue = _queueStorage.GetQueueReference(queueName);

			// 1. GET RAW MESSAGES

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

			// 2. SKIP EMPTY QUEUE

			if (null == rawMessages)
			{
				_countGetMessage.Close(timestamp);
				return new T[0];
			}

			// 3. DESERIALIZE MESSAGE OR MESSAGE WRAPPER, CHECK-OUT

			var messages = new List<T>(count);
			var wrappedMessages = new List<MessageWrapper>();

			lock (_sync)
			{
				foreach (var rawMessage in rawMessages)
				{
					// 3.1. PERSIST POISONED MESSAGES, SKIP

					if (rawMessage.DequeueCount > maxProcessingTrials)
					{
						PersistRawMessage(
							rawMessage,
							queueName,
							PoisonedMessagePersistenceStoreName,
							String.Format("Message was {0} times dequeued but failed processing each time.", rawMessage.DequeueCount - 1));

						continue;
					}

					// 3.2. DESERIALIZE MESSAGE

					object innerMessage;
					using (var stream = new MemoryStream(rawMessage.AsBytes))
					{
						innerMessage = SafeDeserialize<T>(stream);
					}

					// 3.3 CHECK-OUT, COLLECT WRAPPED MESSAGES TO BE UNWRAPPED LATER

					if (innerMessage is T)
					{
						messages.Add((T)innerMessage);

						CheckOutMessage(innerMessage, rawMessage, queueName, false);
					}
					else
					{
						// we don't retrieve messages while holding the lock
						var mw = (MessageWrapper)innerMessage;
						wrappedMessages.Add(mw);

						CheckOutMessage(mw, rawMessage, queueName, true);
					}
				}
			}

			// 4. UNWRAP WRAPPED MESSAGES

			foreach (var mw in wrappedMessages)
			{
				var unwrapTimestamp = _countUnwrapMessage.Open();

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
						CheckInMessage(mw);
					}

					DeleteRawMessage(rawMessage, queue);

					// skipping the message if it can't be unwrapped
					continue;
				}

				T innerMessage = (T)blobContent.Value;

				// substitution: message wrapper replaced by actual item in '_inprocess' list
				CheckOutRelink(mw, innerMessage);

				messages.Add(innerMessage);
				_countUnwrapMessage.Close(unwrapTimestamp);
			}

			_countGetMessage.Close(timestamp);

			// 5. RETURN LIST OF MESSAGES

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
				var timestamp = _countPutMessage.Open();
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

					PutRawMessage(queueMessage, queue);
				}
				_countPutMessage.Close(timestamp);
			}
		}

		byte[] PutOverflowingMessageAndWrap<T>(string queueName, T message)
		{
			var timestamp = _countWrapMessage.Open();

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
				var serializerWrapper = stream.ToArray();

				_countWrapMessage.Close(timestamp);

				return serializerWrapper;
			}
		}

		void DeleteOverflowingMessages(string queueName)
		{
			foreach (var blobName in _blobStorage.List(OverflowingMessagesContainerName, queueName))
			{
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

		public bool Delete<T>(T message)
		{
			return DeleteRange(new[] { message }) > 0;
		}

		public int DeleteRange<T>(IEnumerable<T> messages)
		{
			int deletionCount = 0;

			foreach (var message in messages)
			{
				var timestamp = _countDeleteMessage.Open();

				// 1. GET RAW MESSAGE & QUEUE, OR SKIP IF NOT AVAILABLE/ALREADY DELETED

				CloudQueueMessage rawMessage;
				string queueName;
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
					queueName = inProcMsg.QueueName;
				}

				var queue = _queueStorage.GetQueueReference(queueName);

				// 2. DELETING THE OVERFLOW BLOB, IF WRAPPED

				if (isOverflowing)
				{
					using (var stream = new MemoryStream(rawMessage.AsBytes))
					{
						var mw = (MessageWrapper)_formatter.Deserialize(stream, typeof(MessageWrapper));

						_blobStorage.DeleteBlob(mw.ContainerName, mw.BlobName);
					}
				}

				// 3. DELETE THE MESSAGE FROM THE QUEUE

				if(DeleteRawMessage(rawMessage, queue))
				{
					deletionCount++;
				}

				// 4. REMOVE THE RAW MESSAGE

				CheckInMessage(message);

				_countDeleteMessage.Close(timestamp);
			}

			return deletionCount;
		}

		public bool Abandon<T>(T message)
		{
			return AbandonRange(new[] { message }) > 0;
		}

		public int AbandonRange<T>(IEnumerable<T> messages)
		{
			int abandonCount = 0;

			foreach (var message in messages)
			{
				var timestamp = _countAbandonMessage.Open();

				// 1. GET RAW MESSAGE & QUEUE, OR SKIP IF NOT AVAILABLE/ALREADY DELETED

				CloudQueueMessage oldRawMessage;
				string queueName;

				lock (_sync)
				{
					// ignoring message if already deleted
					InProcessMessage inProcMsg;
					if (!_inProcessMessages.TryGetValue(message, out inProcMsg))
					{
						continue;
					}

					queueName = inProcMsg.QueueName;
					oldRawMessage = inProcMsg.RawMessages[0];
				}

				var queue = _queueStorage.GetQueueReference(queueName);

				// 2. CLONE THE MESSAGE AND PUT IT TO THE QUEUE

				var newRawMessage = new CloudQueueMessage(oldRawMessage.AsBytes);

				try
				{
					_azureServerPolicy.Do(() => queue.AddMessage(newRawMessage));
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
								queue.AddMessage(newRawMessage);
							});
					}
					else
					{
						throw;
					}
				}

				// 3. DELETE THE OLD MESSAGE FROM THE QUEUE

				if(DeleteRawMessage(oldRawMessage, queue))
				{
					abandonCount++;
				}

				// 4. REMOVE THE RAW MESSAGE

				CheckInMessage(message);

				_countAbandonMessage.Close(timestamp);
			}

			return abandonCount;
		}

		public void Persist<T>(T message, string storeName, string reason)
		{
			PersistRange(new[] { message }, storeName, reason);
		}

		public void PersistRange<T>(IEnumerable<T> messages, string storeName, string reason)
		{
			foreach (var message in messages)
			{
				// 1. GET MESSAGE FROM CHECK-OUT, SKIP IF NOT AVAILABLE/ALREADY DELETED

				CloudQueueMessage rawMessage;
				string queueName;

				lock (_sync)
				{
					// ignoring message if already deleted
					InProcessMessage inProcessMessage;
					if (!_inProcessMessages.TryGetValue(message, out inProcessMessage))
					{
						continue;
					}

					queueName = inProcessMessage.QueueName;
					rawMessage = inProcessMessage.RawMessages[0];
				}

				// 2. PERSIST MESSAGE AND DELETE FROM QUEUE

				PersistRawMessage(rawMessage, queueName, storeName, reason);

				// 3. REMOVE MESSAGE FROM CHECK-OUT

				CheckInMessage(message);
			}
		}

		public IEnumerable<string> ListPersisted(string storeName)
		{
			var blobPrefix = PersistedMessageBlobReference.GetPrefix(storeName);
			return _blobStorage.List(blobPrefix).Select(blobReference => blobReference.Key);
		}

		public Maybe<PersistedMessage> GetPersisted(string storeName, string key)
		{
			// 1. GET PERSISTED MESSAGE BLOB

			var blobReference = new PersistedMessageBlobReference(storeName, key);
			var blob = _blobStorage.GetBlob(blobReference);
			if (!blob.HasValue)
			{
				return Maybe<PersistedMessage>.Empty;
			}

			var persistedMessage = blob.Value;
			var data = persistedMessage.Data;
			var dataXml = Maybe<XElement>.Empty;

			// 2. IF WRAPPED, UNWRAP; UNPACK XML IF SUPPORTED

			MessageWrapper wrapper = null;
			try
			{
				using (var stream = new MemoryStream(data))
				{
					wrapper = (MessageWrapper)_formatter.Deserialize(stream, typeof(MessageWrapper));
				}
			}
			catch (SerializationException)
			{
			}

			if (wrapper != null)
			{
				string ignored;
				dataXml = _blobStorage.GetBlobXml(wrapper.ContainerName, wrapper.BlobName, out ignored);
			}
			else
			{
				var formatter = _formatter as IIntermediateBinaryFormatter;
				if (formatter != null)
				{
					using (var stream = new MemoryStream(data))
					{
						dataXml = formatter.UnpackXml(stream);
					}
				}
			}

			// 3. RETURN

			return new PersistedMessage
				{
					QueueName = persistedMessage.QueueName,
					StoreName = storeName,
					Key = key,
					InsertionTime = persistedMessage.InsertionTime,
					PersistenceTime = persistedMessage.PersistenceTime,
					DequeueCount = persistedMessage.DequeueCount,
					Reason = persistedMessage.Reason,
					DataXml = dataXml,
				};
		}

		public void DeletePersisted(string storeName, string key)
		{
			// 1. GET PERSISTED MESSAGE BLOB

			var blobReference = new PersistedMessageBlobReference(storeName, key);
			var blob = _blobStorage.GetBlob(blobReference);
			if (!blob.HasValue)
			{
				return;
			}

			var persistedMessage = blob.Value;

			// 2. IF WRAPPED, UNWRAP AND DELETE BLOB

			MessageWrapper wrapper = null;
			try
			{
				using (var stream = new MemoryStream(persistedMessage.Data))
				{
					wrapper = (MessageWrapper)_formatter.Deserialize(stream, typeof(MessageWrapper));
				}
			}
			catch (SerializationException)
			{
			}

			if (wrapper != null)
			{
				_blobStorage.DeleteBlob(wrapper.ContainerName, wrapper.BlobName);
			}

			// 3. DELETE PERSISTED MESSAGE

			_blobStorage.DeleteBlob(blobReference);
		}

		public void RestorePersisted(string storeName, string key)
		{
			// 1. GET PERSISTED MESSAGE BLOB

			var blobReference = new PersistedMessageBlobReference(storeName, key);
			var blob = _blobStorage.GetBlob(blobReference);
			if(!blob.HasValue)
			{
				return;
			}

			var persistedMessage = blob.Value;

			// 2. PUT MESSAGE TO QUEUE

			var queue = _queueStorage.GetQueueReference(persistedMessage.QueueName);
			var rawMessage = new CloudQueueMessage(persistedMessage.Data);
			PutRawMessage(rawMessage, queue);

			// 3. DELETE PERSISTED MESSAGE

			_blobStorage.DeleteBlob(blobReference);
		}

		void PersistRawMessage(CloudQueueMessage message, string queueName, string storeName, string reason)
		{
			var timestamp = _countPersistMessage.Open();

			var queue = _queueStorage.GetQueueReference(queueName);

			// 1. PERSIST MESSAGE TO BLOB

			var blobReference = PersistedMessageBlobReference.GetNew(storeName);
			var persistedMessage = new PersistedMessageData
			{
				QueueName = queueName,
				InsertionTime = message.InsertionTime.Value,
				PersistenceTime = DateTimeOffset.UtcNow,
				DequeueCount = message.DequeueCount,
				Reason = reason,
				Data = message.AsBytes,
			};

			_blobStorage.PutBlob(blobReference, persistedMessage);

			// 2. DELETE MESSAGE FROM QUEUE

			DeleteRawMessage(message, queue);

			_countPersistMessage.Close(timestamp);
		}

		bool DeleteRawMessage(CloudQueueMessage message, CloudQueue queue)
		{
			try
			{
				_azureServerPolicy.Do(() => queue.DeleteMessage(message));
				return true;
			}
			catch (StorageClientException ex)
			{
				if (ex.ErrorCode != StorageErrorCode.ResourceNotFound
					&& ex.ExtendedErrorInformation.ErrorCode != QueueErrorCodeStrings.QueueNotFound)
				{
					throw;
				}
			}

			return false;
		}

		void PutRawMessage(CloudQueueMessage message, CloudQueue queue)
		{
			try
			{
				_azureServerPolicy.Do(() => queue.AddMessage(message));
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
						queue.AddMessage(message);
					});
				}
				else
				{
					throw;
				}
			}
		}

		void CheckOutMessage(object message, CloudQueueMessage rawMessage, string queueName, bool isOverflowing)
		{
			lock (_sync)
			{
				// If T is a value type, _inprocess could already contain the message
				// (not the same exact instance, but an instance that is value-equal to this one)
				InProcessMessage inProcessMessage;
				if (!_inProcessMessages.TryGetValue(message, out inProcessMessage))
				{
					inProcessMessage = new InProcessMessage
						{
							QueueName = queueName,
							RawMessages = new List<CloudQueueMessage> {rawMessage},
							IsOverflowing = isOverflowing
						};
					_inProcessMessages.Add(message, inProcessMessage);
				}
				else
				{
					inProcessMessage.RawMessages.Add(rawMessage);
				}
			}
		}

		void CheckOutRelink(object originalMessage, object newMessage)
		{
			lock (_sync)
			{
				var inProcessMessage = _inProcessMessages[originalMessage];
				_inProcessMessages.Remove(originalMessage);
				_inProcessMessages.Add(newMessage, inProcessMessage);
			}
		}

		void CheckInMessage(object message)
		{
			lock (_sync)
			{
				var inProcessMessage = _inProcessMessages[message];
				inProcessMessage.RawMessages.RemoveAt(0);

				if (0 == inProcessMessage.RawMessages.Count)
				{
					_inProcessMessages.Remove(message);
				}
			}
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
		/// <summary>Name of the queue where messages are originating from.</summary>
		public string QueueName { get; set; }

		/// <summary>The multiple, different raw <see cref="CloudQueueMessage" /> 
		/// objects as returned from the queue storage.</summary>
		public List<CloudQueueMessage> RawMessages { get; set; }

		/// <summary>A flag indicating whether the original message was bigger than the max 
		/// allowed size and was  therefore wrapped in <see cref="MessageWrapper" />.</summary>
		public bool IsOverflowing { get; set; }
	}

	internal class OverflowingMessageBlobReference<T> : BlobReference<T>
	{
		public override string ContainerName
		{
			get { return QueueStorageProvider.OverflowingMessagesContainerName; }
		}

		/// <summary>Indicates the name of the queue where the message has been originally pushed.</summary>
		[UsedImplicitly, Rank(0)]
		public string QueueName;

		/// <summary>Message identifiers as specified by the queue storage itself.</summary>
		[UsedImplicitly, Rank(1)]
		public Guid MessageId;

		OverflowingMessageBlobReference(string queueName, Guid guid)
		{
			QueueName = queueName;
			MessageId = guid;
		}

		/// <summary>Used to iterate over all the overflowing messages 
		/// associated to a queue.</summary>
		public static OverflowingMessageBlobReference<T> GetNew(string queueName)
		{
			return new OverflowingMessageBlobReference<T>(queueName, Guid.NewGuid());
		}
	}

	[DataContract]
	internal class PersistedMessageData
	{
		[DataMember(Order = 1)]
		public string QueueName { get; set; }

		[DataMember(Order = 2)]
		public DateTimeOffset InsertionTime { get; set; }

		[DataMember(Order = 3)]
		public DateTimeOffset PersistenceTime { get; set; }

		[DataMember(Order = 4)]
		public int DequeueCount { get; set; }

		[DataMember(Order = 5, IsRequired = false)]
		public string Reason { get; set; }

		[DataMember(Order = 6)]
		public byte[] Data { get; set; }
	}

	internal class PersistedMessageBlobReference : BlobReference<PersistedMessageData>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-persisted-messages"; }
		}

		/// <summary>Indicates the name of the swap out store where the message is persisted.</summary>
		[UsedImplicitly, Rank(0)]
		public string StoreName;

		[UsedImplicitly, Rank(1)]
		public string Key;

		public PersistedMessageBlobReference(string storeName, string key)
		{
			StoreName = storeName;
			Key = key;
		}

		public static PersistedMessageBlobReference GetNew(string storeName, string key)
		{
			return new PersistedMessageBlobReference(storeName, key);
		}

		public static PersistedMessageBlobReference GetNew(string storeName)
		{
			return new PersistedMessageBlobReference(storeName, Guid.NewGuid().ToString("N"));
		}

		public static BlobNamePrefix<PersistedMessageBlobReference> GetPrefix(string storeName)
		{
			return GetPrefix(new PersistedMessageBlobReference(storeName, null), 1);
		}
	}
}
