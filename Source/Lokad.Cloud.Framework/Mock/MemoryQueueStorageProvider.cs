﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Mock
{
	/// <summary>Mock in-memory Queue Storage.</summary>
	public class MemoryQueueStorageProvider : IQueueStorageProvider
	{
		/// <summary>Root used to synchronize accesses to <c>_inprocess</c>. 
		///</summary>
		readonly object _sync = new object();

		readonly Dictionary<string, Queue<object>> _queueStorage;
		readonly Dictionary<string, HashSet<object>> _queuesHashset;
		readonly IBinaryFormatter _formatter;

		public MemoryQueueStorageProvider(IBinaryFormatter formatter)
		{
			_queueStorage = new Dictionary<string, Queue<object>>();
			_queuesHashset = new Dictionary<string, HashSet<object>>();
			_formatter = formatter;
		}

		public IEnumerable<string> List(string prefix)
		{
			return _queueStorage.Keys.Where(e => e.StartsWith(prefix));
		}

		public IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout)
		{
			lock (_sync)
			{
				var items = new List<T>(count);
				for (int i = 0; i < count; i++)
				{
					if (_queueStorage[queueName].Any())
						items.Add((T) _queueStorage[queueName].Dequeue());
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

				if ( ! _queueStorage.ContainsKey(queueName))
				{
					_queueStorage.Add(queueName, new Queue<object>());
					_queuesHashset.Add(queueName, new HashSet<object>());
				}

				_queueStorage[queueName].Enqueue(message);
				_queuesHashset[queueName].Add(message);
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
				_queueStorage[queueName].Clear();
				_queuesHashset[queueName].Clear();
			}
		}

		public bool Delete<T>(string queueName, T message)
		{
			lock (_sync)
			{
				if (!_queuesHashset[queueName].Contains(message))
				{
					return false;
				}

				_queuesHashset[queueName].Remove(message);
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

		public bool DeleteQueue(string queueName)
		{
			lock (_sync)
			{
				if (!_queueStorage.ContainsKey(queueName))
				{
					return false;
				}

				_queueStorage.Remove(queueName);
				_queuesHashset.Remove(queueName);
				return true;
			}
		}

		public int GetApproximateCount(string queueName)
		{
			lock (_sync)
			{
				Queue<object> queue;
				return _queueStorage.TryGetValue(queueName, out queue)
					? queue.Count : 0;
			}
		}
	}
}
