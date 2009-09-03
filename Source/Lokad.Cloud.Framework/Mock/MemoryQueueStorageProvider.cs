#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Mock
{
	/// <summary>Mock in-memory Queue Storage.</summary>
	public class MemoryQueueStorageProvider : IQueueStorageProvider
	{
		readonly Dictionary<string, Queue<object>> _queueStorage;
		readonly Dictionary<string, HashSet<object>> _queuesHashset;
		readonly IFormatter _formatter;

		public MemoryQueueStorageProvider(IFormatter formatter)
		{
			_queueStorage = new Dictionary<string, Queue<object>>();
			_queuesHashset = new Dictionary<string, HashSet<object>>();
			_formatter = formatter;
		}

		public IEnumerable<string> List(string prefix)
		{
			return _queueStorage.Keys.Where(e => e.StartsWith(prefix));
		}

		public IEnumerable<T> Get<T>(string queueName, int count)
		{
			var items = new List<T>(count);
			for (int i = 0 ; i < count; i++)
			{
				if (_queueStorage[queueName].Any())
					items.Add((T)_queueStorage[queueName].Dequeue());
			}
			return items;
		}

		public void Put<T>(string queueName, T message)
		{
			PutRange(queueName, new[] { message });
		}

		public void PutRange<T>(string queueName, IEnumerable<T> messages)
		{
			var stream = new MemoryStream();
			messages.ForEach(message => _formatter.Serialize(stream, message) ); //Checking the messages are serializable.
			messages.ForEach(message => _queueStorage[queueName].Enqueue(message));
			messages.ForEach(message=> _queuesHashset[queueName].Add(message));
		}

		public void Clear(string queueName)
		{
			_queueStorage[queueName].Clear();
			_queuesHashset[queueName].Clear();
		}

		public bool Delete<T>(string queueName, T message)
		{
			return  DeleteRange(queueName, new[] {message}) == 1 ? true : false;
		}

		public int DeleteRange<T>(string queueName, IEnumerable<T> messages)
		{
			int counter = messages.Where(e =>
				{
					if (_queuesHashset[queueName].Contains(e))
					{
						_queuesHashset[queueName].Remove(e);
						return true;
					}
					else
					{
						return false;
					}
				}).Count();

			return counter;
		}

		public bool DeleteQueue(string queueName)
		{
			if (_queueStorage.ContainsKey(queueName))
			{
				_queueStorage.Remove(queueName);
				_queuesHashset.Remove(queueName);
				return true;
			}
			else
			{
				return false;
			}
		}

		public int GetApproximateCount(string queueName)
		{
			return _queueStorage[queueName].Count;
		}
	}
}
