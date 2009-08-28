#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using Lokad.Cloud.Core;

namespace Lokad.Cloud.Mock
{
	/// <summary>Mock in-memory Queue Storage.</summary>
	public class MemoryQueueStorageProvider : IQueueStorageProvider
	{
		IEnumerable<string> IQueueStorageProvider.List(string prefix)
		{
			throw new NotImplementedException();
		}

		IEnumerable<T> IQueueStorageProvider.Get<T>(string queueName, int count)
		{
			throw new NotImplementedException();
		}

		void IQueueStorageProvider.Put<T>(string queueName, T message)
		{
			throw new NotImplementedException();
		}

		void IQueueStorageProvider.PutRange<T>(string queueName, IEnumerable<T> messages)
		{
			throw new NotImplementedException();
		}

		void IQueueStorageProvider.Clear(string queueName)
		{
			throw new NotImplementedException();
		}

		bool IQueueStorageProvider.Delete<T>(string queueName, T message)
		{
			throw new NotImplementedException();
		}

		int IQueueStorageProvider.DeleteRange<T>(string queueName, IEnumerable<T> messages)
		{
			throw new NotImplementedException();
		}

		bool IQueueStorageProvider.DeleteQueue(string queueName)
		{
			throw new NotImplementedException();
		}

		int IQueueStorageProvider.GetApproximateCount(string queueName)
		{
			throw new NotImplementedException();
		}
	}
}
