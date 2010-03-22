#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Storage;
using NUnit.Framework;

namespace Lokad.Cloud.Mock.Test
{
	[TestFixture]
	public class MemoryQueueStorageProviderTests
	{
		[Test]
		public void GetOnMissingQueueDoesNotFail()
		{
			var queueStorage = new MemoryQueueStorageProvider();
			queueStorage.Get<int>("nosuchqueue", 1);
		}

		[Test]
		public void ItemsGetPutInMonoThread()
		{
			var queueStorage = new MemoryQueueStorageProvider();
			var fakeMessages = Enumerable.Range(0, 3).Select(i => new FakeMessage(i)).ToArray();

			var firstQueueName = "firstQueueName";
			var secondQueueName = "secondQueueName";

			queueStorage.PutRange(firstQueueName, fakeMessages.Take(2));
			queueStorage.PutRange(secondQueueName, fakeMessages.Skip(2).ToArray());

			Assert.AreEqual(2, queueStorage.GetApproximateCount(firstQueueName), "#A04 First queue has not the right number of elements.");
			Assert.AreEqual(1, queueStorage.GetApproximateCount(secondQueueName), "#A05 Second queue has not the right number of elements.");
		}

		[Test]
		public void ItemsReturnedInMonoThread()
		{
			var queueStorage = new MemoryQueueStorageProvider();
			var fakeMessages = Enumerable.Range(0, 10).Select(i => new FakeMessage(i)).ToArray();

			var firstQueueName = "firstQueueName";

			queueStorage.PutRange(firstQueueName, fakeMessages.Take(6));
			var allFirstItems = queueStorage.Get<FakeMessage>(firstQueueName, 6);
			queueStorage.Clear(firstQueueName);

			queueStorage.PutRange(firstQueueName, fakeMessages.Take(6));
			var partOfFirstItems = queueStorage.Get<FakeMessage>(firstQueueName, 2);
			Assert.AreEqual(4, queueStorage.GetApproximateCount(firstQueueName), "#A06");
			queueStorage.Clear(firstQueueName);

			queueStorage.PutRange(firstQueueName, fakeMessages.Take(6));
			var allFirstItemsAndMore = queueStorage.Get<FakeMessage>(firstQueueName, 8);
			queueStorage.Clear(firstQueueName);

			Assert.AreEqual(6, allFirstItems.Count(), "#A07");
			Assert.AreEqual(2, partOfFirstItems.Count(), "#A08");
			Assert.AreEqual(6, allFirstItemsAndMore.Count(), "#A09");
			
		}

		[Test]
		public void ListInMonoThread()
		{
			var queueStorage = new MemoryQueueStorageProvider();
			var fakeMessages = Enumerable.Range(0, 10).Select(i => new FakeMessage(i)).ToArray();

			var firstQueueName = "firstQueueName";

			queueStorage.PutRange(firstQueueName, fakeMessages.Take(6));
			var queuesName = queueStorage.List("");

			Assert.AreEqual(1, queuesName.Count(), "#A010");
		}

		[Serializable]
		class FakeMessage
		{
			double Value { get; set; }

			public FakeMessage(double value)
			{
				Value = value;
			}
		}
	}
}
