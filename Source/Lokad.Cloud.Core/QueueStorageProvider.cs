#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Core
{
	/// <summary>Provides access to the Queue Storage (plus the Blob Storage when
	/// messages are overflowing).</summary>
	/// <remarks>
	/// All the methods of <see cref="QueueStorageProvider"/> are thread-safe.
	/// </remarks>
	public class QueueStorageProvider : IQueueStorageProvider
	{
		// root for synchronized access
		private object _sync = new object();

		private QueueStorage _queueStorage;
		private BlobStorage _blobStorage;
		private ActionPolicy _policy;
		private IFormatter _formatter;

		// messages currently being processed
		private Dictionary<object, Message> _inprocess;

		/// <summary>IoC constructor.</summary>
		public QueueStorageProvider(
			QueueStorage queueStorage, BlobStorage blobStorage, ActionPolicy policy, IFormatter formatter)
		{
			_queueStorage = queueStorage;
			_blobStorage = blobStorage;
			_policy = policy;
			_formatter = formatter;
		}

		public IEnumerable<T> Get<T>(string queueName, int count)
		{
			var queue = _queueStorage.GetQueue(queueName);

			var rawMessages = queue.GetMessages(count);

			var messages = new List<T>(rawMessages.Count());

			lock(_sync)
			{
				foreach(var rawMessage in rawMessages)
				{
					var stream = new MemoryStream(rawMessage.ContentAsBytes());

					var innerMessage = _formatter.Deserialize(stream);

					if(innerMessage is T)
					{
						messages.Add((T)innerMessage);
						_inprocess.Add(innerMessage, rawMessage);
					}
					else
					{
						// TODO: need to deal with message wrapper here
						var wrapper = (MessageWrapper) innerMessage;

						throw new NotImplementedException();
					}
				}
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

				if(buffer.Length < Message.MaxMessageSize)
				{
					try
					{
						queue.PutMessage(new Message(buffer));
					}
					catch(StorageClientException ex)
					{
						// HACK: not storage status error code yet
						if (ex.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound)
						{
							queue.CreateQueue();

							// It usually takes time before the queue gets available
							_policy.Do(() => queue.PutMessage(new Message(buffer)));
						}
					}
				}
				else
				{
					// TODO: overflowing messages should be handled.
					throw new NotImplementedException();
				}
			}
		}

		public void Delete<T>(string queueName, IEnumerable<T> messages)
		{
			var queue = _queueStorage.GetQueue(queueName);

			foreach(var message in messages)
			{
				Message rawMessage;
 
				lock(_sync)
				{
					rawMessage = _inprocess[message];
				}

				queue.DeleteMessage(rawMessage);

				lock(_sync)
				{
					_inprocess.Remove(message);
				}
			}
		}
	}
}
