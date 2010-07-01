#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lokad.Cloud.Storage;
using Lokad.Serialization;

namespace Lokad.Cloud.Mock
{
	/// <summary>Mock in-memory Queue Storage.</summary>
	public class MemoryQueueStorageProvider : IQueueStorageProvider
	{
		/// <summary>Root used to synchronize accesses to <c>_inprocess</c>.</summary>
		readonly object _sync = new object();

		readonly Dictionary<string,Queue<object>> _queues;
		readonly HashSet<Pair<string,object>> _inProgressMessages;
		readonly HashSet<Quad<string,string,string,object>> _persistedMessages;
		readonly IDataSerializer _serializer;
		
		/// <summary>Default constructor.</summary>
		public MemoryQueueStorageProvider() 
		{
			_queues = new Dictionary<string, Queue<object>>();
			_inProgressMessages = new HashSet<Pair<string, object>>();
			_persistedMessages = new HashSet<Quad<string, string, string, object>>();
			_serializer = new CloudFormatter();
		}

		public IEnumerable<string> List(string prefix)
		{
			return _queues.Keys.Where(e => e.StartsWith(prefix));
		}

		public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout, int maxProcessingTrials)
		{
			lock (_sync)
			{
				var items = new List<T>(count);
				for (int i = 0; i < count; i++)
				{
					if (_queues.ContainsKey(queueName) && _queues[queueName].Any())
					{
						var message = _queues[queueName].Dequeue();
						_inProgressMessages.Add(Tuple.From(queueName, message));
						items.Add((T)message);
					}
				}
				return items;
			}
		}

		public void Put<T>(string queueName, T message)
		{
			lock (_sync)
			{
				using (var stream = new MemoryStream())
				{
					_serializer.Serialize(message, stream);
				} //Checking the message is serializable.

				if (!_queues.ContainsKey(queueName))
				{
					_queues.Add(queueName, new Queue<object>());
				}

				_queues[queueName].Enqueue(message);
			}
		}

		public void PutRange<T>(string queueName, IEnumerable<T> messages)
		{
			lock (_sync)
			{
				messages.ForEach(message => Put(queueName, message));
			}
		}

		public void Clear(string queueName)
		{
			lock (_sync)
			{
				_queues[queueName].Clear();
				_inProgressMessages.RemoveWhere(p => p.Key == queueName);
			}
		}

		public bool Delete<T>(T message)
		{
			lock (_sync)
			{
				var entry = _inProgressMessages.FirstOrEmpty(p => p.Value == (object) message);
				if (!entry.HasValue)
				{
					return false;
				}

				_inProgressMessages.Remove(entry.Value);
				return true;
			}
		}

		public int DeleteRange<T>(IEnumerable<T> messages)
		{
			lock (_sync)
			{
				return messages.Where(Delete).Count();
			}
		}

		public bool Abandon<T>(T message)
		{
			lock (_sync)
			{
				var firstOrEmpty = _inProgressMessages.FirstOrEmpty(p => p.Value == (object) message);
				if (!firstOrEmpty.HasValue)
				{
					return false;
				}

				// Add back to queue
				var entry = firstOrEmpty.Value;
				if (!_queues.ContainsKey(entry.Key))
				{
					_queues.Add(entry.Key, new Queue<object>());
				}

				_queues[entry.Key].Enqueue(entry.Value);

				// Remove from invisible queue
				_inProgressMessages.Remove(entry);

				return true;
			}
		}

		public int AbandonRange<T>(IEnumerable<T> messages)
		{
			lock (_sync)
			{
				return messages.Where(Abandon).Count();
			}
		}

		public void Persist<T>(T message, string storeName, string reason)
		{
			lock (_sync)
			{
				var firstOrEmpty = _inProgressMessages.FirstOrEmpty(p => p.Value == (object) message);
				if (!firstOrEmpty.HasValue)
				{
					return;
				}

				// persist
				var key = Guid.NewGuid().ToString("N");
				_persistedMessages.Add(Tuple.From(storeName, key, firstOrEmpty.Value.Key, (object) message));

				// Remove from invisible queue
				_inProgressMessages.Remove(firstOrEmpty.Value);
			}
		}

		public void PersistRange<T>(IEnumerable<T> messages, string storeName, string reason)
		{
			lock (_sync)
			{
				foreach(var message in messages)
				{
					Persist(message, storeName, reason);
				}
			}
		}

		public IEnumerable<string> ListPersisted(string storeName)
		{
			lock (_sync)
			{
				return _persistedMessages
					.Where(x => x.Item1 == storeName)
					.Select(x => x.Item2)
					.ToArray();
			}
		}

		public Maybe<PersistedMessage> GetPersisted(string storeName, string key)
		{
			lock (_sync)
			{
				var tuple = _persistedMessages.FirstOrEmpty(x => x.Item1 == storeName && x.Item2 == key);
				return tuple.Convert(x => new PersistedMessage
					{
						QueueName = x.Item3,
						StoreName = x.Item1,
						Key = x.Item2
					});
			}
		}

		public void DeletePersisted(string storeName, string key)
		{
			lock (_sync)
			{
				_persistedMessages.RemoveWhere(x => x.Item1 == storeName && x.Item2 == key);
			}
		}

		public void RestorePersisted(string storeName, string key)
		{
			lock (_sync)
			{
				var item = _persistedMessages.First(x => x.Item1 == storeName && x.Item2 == key);
				_persistedMessages.Remove(item);

				if (!_queues.ContainsKey(item.Item3))
				{
					_queues.Add(item.Item3, new Queue<object>());
				}

				_queues[item.Item3].Enqueue(item.Item4);

			}
		}

		public bool DeleteQueue(string queueName)
		{
			lock (_sync)
			{
				if (!_queues.ContainsKey(queueName))
				{
					return false;
				}

				_queues.Remove(queueName);
				_inProgressMessages.RemoveWhere(p => p.Key == queueName);
				return true;
			}
		}

		public int GetApproximateCount(string queueName)
		{
			lock (_sync)
			{
				Queue<object> queue;
				return _queues.TryGetValue(queueName, out queue)
					? queue.Count : 0;
			}
		}

		public Maybe<TimeSpan> GetApproximateLatency(string queueName)
		{
			return Maybe<TimeSpan>.Empty;
		}
	}
}
