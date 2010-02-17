#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lokad.Cloud.Mock
{
	/// <summary>Mock in-memory Queue Storage.</summary>
	public class MemoryQueueStorageProvider : IQueueStorageProvider
	{
		/// <summary>Root used to synchronize accesses to <c>_inprocess</c>. 
		///</summary>
		readonly object _sync = new object();

		readonly Dictionary<string, Queue<object>> _queues;
		readonly HashSet<Pair<string,object>> _inProgressMessages;
		readonly IBinaryFormatter _formatter;

		public MemoryQueueStorageProvider(IBinaryFormatter formatter)
		{
			_queues = new Dictionary<string, Queue<object>>();
			_inProgressMessages = new HashSet<Pair<string, object>>();
			_formatter = formatter;
		}

		public IEnumerable<string> List(string prefix)
		{
			return _queues.Keys.Where(e => e.StartsWith(prefix));
		}

		public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout)
		{
			lock (_sync)
			{
				var items = new List<T>(count);
				for (int i = 0; i < count; i++)
				{
					if (_queues[queueName].Any())
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
					_formatter.Serialize(stream, message);
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

		public bool Delete<T>(string queueName, T message)
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

		public int DeleteRange<T>(string queueName, IEnumerable<T> messages)
		{
			lock (_sync)
			{
				return messages.Where(e => Delete(queueName, e)).Count();
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
	}
}
