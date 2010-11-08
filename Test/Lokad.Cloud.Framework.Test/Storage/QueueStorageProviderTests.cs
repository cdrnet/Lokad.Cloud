#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.Azure;
using Lokad.Cloud.Test;
using NUnit.Framework;
using System.Text;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Azure.Test
{
	[TestFixture]
	public class QueueStorageProviderTests
	{
		private const string OverflowingContainerName = "lokad-cloud-overflowing-messages";
		private const string BaseQueueName = "tests-queuestorageprovider-";
		private string QueueName;

		private static Random _rand = new Random();

		[SetUp]
		public void Setup()
		{
			QueueName = BaseQueueName + Guid.NewGuid().ToString("N");
		}

		[TearDown]
		public void TearDown()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();
			provider.DeleteQueue(QueueName);
		}

		[Test]
		public void PutGetDelete()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

			Assert.IsNotNull(provider, "#A00");

			var message = new MyMessage();

			provider.DeleteQueue(QueueName); // deleting queue on purpose 
			// (it's slow but necessary to really validate the retry policy)

			provider.Put(QueueName, message);
			var retrieved = provider.Get<MyMessage>(QueueName, 1).First();

			Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

			provider.Delete(retrieved);
		}

		[Test]
		public void PutGetDeleteOverflowing()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

			Assert.IsNotNull(provider, "#A00");

			// 20k chosen so that it doesn't fit into the queue.
			var message = new MyMessage { MyBuffer = new byte[20000] };

			// fill buffer with random content
			_rand.NextBytes(message.MyBuffer);

			provider.Clear(QueueName);

			provider.Put(QueueName, message);
			var retrieved = provider.Get<MyMessage>(QueueName, 1).First();

			Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");
			CollectionAssert.AreEquivalent(message.MyBuffer, retrieved.MyBuffer, "#A02");

			for (int i = 0; i < message.MyBuffer.Length; i++)
			{
				Assert.AreEqual(message.MyBuffer[i], retrieved.MyBuffer[i], "#A02-" + i);
			}

			provider.Delete(retrieved);
		}

		[Test]
		public void PutGetDeleteIdenticalStructOrNative()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

			Assert.IsNotNull(provider, "GlobalSetup should resolve the provider");

			var testStruct = new MyStruct()
			{
				IntegerValue = 12,
				StringValue = "hello"
			};

			for (int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testStruct);
			}

			var outStruct1 = provider.Get<MyStruct>(QueueName, 1).First();
			var outStruct2 = provider.Get<MyStruct>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(outStruct1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(outStruct2), "2nd Delete failed");
			Assert.IsFalse(provider.Delete(outStruct2), "3nd Delete succeeded");

			var outAllStructs = provider.Get<MyStruct>(QueueName, 20);
			Assert.AreEqual(8, outAllStructs.Count(), "Wrong queue item count");
			foreach (var str in outAllStructs)
			{
				Assert.AreEqual(testStruct.IntegerValue, str.IntegerValue, "Wrong integer value");
				Assert.AreEqual(testStruct.StringValue, str.StringValue, "Wrong string value");
				Assert.IsTrue(provider.Delete(str), "Delete failed");
			}

			var testDouble = 3.6D;

			for (int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testDouble);
			}

			var outDouble1 = provider.Get<double>(QueueName, 1).First();
			var outDouble2 = provider.Get<double>(QueueName, 1).First();
			var outDouble3 = provider.Get<double>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(outDouble1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(outDouble2), "2nd Delete failed");
			Assert.IsTrue(provider.Delete(outDouble3), "3nd Delete failed");
			Assert.IsFalse(provider.Delete(outDouble2), "3nd Delete succeeded");

			var outAllDoubles = provider.Get<double>(QueueName, 20);
			Assert.AreEqual(7, outAllDoubles.Count(), "Wrong queue item count");
			foreach (var dbl in outAllDoubles)
			{
				Assert.AreEqual(testDouble, dbl, "Wrong double value");
				Assert.IsTrue(provider.Delete(dbl), "Delete failed");
			}

			var testString = "hi there!";

			for (int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testString);
			}

			var outString1 = provider.Get<string>(QueueName, 1).First();
			var outString2 = provider.Get<string>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(outString1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(outString2), "2nd Delete failed");
			Assert.IsFalse(provider.Delete(outString2), "3nd Delete succeeded");

			var outAllStrings = provider.Get<string>(QueueName, 20);
			Assert.AreEqual(8, outAllStrings.Count(), "Wrong queue item count");
			foreach (var str in outAllStrings)
			{
				Assert.AreEqual(testString, str, "Wrong string value");
				Assert.IsTrue(provider.Delete(str), "Delete failed");
			}

			var testClass = new StringBuilder("text");

			for (int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testClass);
			}

			var outClass1 = provider.Get<StringBuilder>(QueueName, 1).First();
			var outClass2 = provider.Get<StringBuilder>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(outClass1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(outClass2), "2nd Delete failed");
			Assert.IsFalse(provider.Delete(outClass2), "3nd Delete succeeded");

			var outAllClasses = provider.Get<StringBuilder>(QueueName, 20);
			Assert.AreEqual(8, outAllClasses.Count(), "Wrong queue item count");
			foreach (var cls in outAllClasses)
			{
				Assert.AreEqual(testClass.ToString(), cls.ToString(), "Wrong deserialized class value");
				Assert.IsTrue(provider.Delete(cls), "Delete failed");
			}
		}

		// TODO: create same unit test for Clear()

		[Test]
		public void DeleteRemovesOverflowingBlobs()
		{
			var queueProvider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();
			var blobProvider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

			var queueName = "test1-" + Guid.NewGuid().ToString("N");

			// CAUTION: we are now compressing serialization output.
			// hence, we can't just pass an empty array, as it would be compressed at near 100%.

			var data = new byte[20000];
			_rand.NextBytes(data);

			queueProvider.Put(queueName, data);

			// HACK: implicit pattern for listing overflowing messages
			var overflowingCount = blobProvider.List(OverflowingContainerName, queueName).Count();

			Assert.AreEqual(1, overflowingCount, "#A00");

			queueProvider.DeleteQueue(queueName);

			overflowingCount = blobProvider.List(OverflowingContainerName, queueName).Count();

			Assert.AreEqual(0, overflowingCount, "#A01");
		}

		[Test]
		public void ClearRemovesOverflowingBlobs()
		{
			var queueProvider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();
			var blobProvider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

			var queueName = "test1-" + Guid.NewGuid().ToString("N");

			// CAUTION: we are now compressing serialization output.
			// hence, we can't just pass an empty array, as it would be compressed at near 100%.

			var data = new byte[20000];
			_rand.NextBytes(data);

			queueProvider.Put(queueName, data);

			// HACK: implicit pattern for listing overflowing messages
			var overflowingCount = blobProvider.List(OverflowingContainerName, queueName).Count();

			Assert.AreEqual(1, overflowingCount, "#A00");

			queueProvider.Clear(queueName);

			overflowingCount = blobProvider.List(OverflowingContainerName, queueName).Count();

			Assert.AreEqual(0, overflowingCount, "#A01");

			queueProvider.DeleteQueue(queueName);
		}

		[Test]
		public void PutGetAbandonDelete()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

			Assert.IsNotNull(provider, "#A00");

			var message = new MyMessage();

			provider.DeleteQueue(QueueName); // deleting queue on purpose 
			// (it's slow but necessary to really validate the retry policy)

			// put
			provider.Put(QueueName, message);

			// get
			var retrieved = provider.Get<MyMessage>(QueueName, 1).First();
			Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

			// abandon
			var abandoned = provider.Abandon(retrieved);
			Assert.IsTrue(abandoned, "#A02");

			// abandon II should fail (since not invisible)
			var abandoned2 = provider.Abandon(retrieved);
			Assert.IsFalse(abandoned2, "#A03");

			// get again
			var retrieved2 = provider.Get<MyMessage>(QueueName, 1).First();
			Assert.AreEqual(message.MyGuid, retrieved2.MyGuid, "#A04");

			// delete
			var deleted = provider.Delete(retrieved2);
			Assert.IsTrue(deleted, "#A05");

			// get now should fail
			var retrieved3 = provider.Get<MyMessage>(QueueName, 1).FirstOrEmpty();
			Assert.IsFalse(retrieved3.HasValue, "#A06");

			// abandon does not put it to the queue again
			var abandoned3 = provider.Abandon(retrieved2);
			Assert.IsFalse(abandoned3, "#A07");

			// get now should still fail
			var retrieved4 = provider.Get<MyMessage>(QueueName, 1).FirstOrEmpty();
			Assert.IsFalse(retrieved4.HasValue, "#A07");
		}

		[Test]
		public void PersistRestore()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();
			const string storeName = "TestStore";

			Assert.IsNotNull(provider, "#A00");

			var message = new MyMessage();

			// clean up
			provider.DeleteQueue(QueueName);
			foreach (var skey in provider.ListPersisted(storeName))
			{
				provider.DeletePersisted(storeName, skey);
			}

			// put
			provider.Put(QueueName, message);

			// get
			var retrieved = provider.Get<MyMessage>(QueueName, 1).First();
			Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

			// persist
			provider.Persist(retrieved, storeName, "manual test");

			// abandon should fail (since not invisible anymore)
			Assert.IsFalse(provider.Abandon(retrieved), "#A02");

			// list persisted message
			var key = provider.ListPersisted(storeName).Single();

			// get persisted message
			var persisted = provider.GetPersisted(storeName, key);
			Assert.IsTrue(persisted.HasValue, "#A03");
			Assert.IsTrue(persisted.Value.DataXml.HasValue, "#A04");
			var xml = persisted.Value.DataXml.Value;
			var property = xml.Elements().Single(x => x.Name.LocalName == "MyGuid");
			Assert.AreEqual(message.MyGuid, new Guid(property.Value), "#A05");

			// restore persisted message
			provider.RestorePersisted(storeName, key);

			// list no longer contains key
			Assert.IsFalse(provider.ListPersisted(storeName).Any(), "#A06");

			// get
			var retrieved2 = provider.Get<MyMessage>(QueueName, 1).First();
			Assert.AreEqual(message.MyGuid, retrieved2.MyGuid, "#A07");

			// delete
			Assert.IsTrue(provider.Delete(retrieved2), "#A08");
		}

		[Test]
		public void PersistRestoreOverflowing()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();
			var blobProvider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			const string storeName = "TestStore";

			Assert.IsNotNull(provider, "#A00");

			// CAUTION: we are now compressing serialization output.
			// hence, we can't just pass an empty array, as it would be compressed at near 100%.

			var data = new byte[20000];
			_rand.NextBytes(data);

			// clean up
			provider.DeleteQueue(QueueName);
			foreach (var skey in provider.ListPersisted(storeName))
			{
				provider.DeletePersisted(storeName, skey);
			}

			// put
			provider.Put(QueueName, data);

			Assert.AreEqual(1, blobProvider.List(OverflowingContainerName, QueueName).Count(), "#A01");

			// get
			var retrieved = provider.Get<byte[]>(QueueName, 1).First();

			// persist
			provider.Persist(retrieved, storeName, "manual test");

			Assert.AreEqual(1, blobProvider.List(OverflowingContainerName, QueueName).Count(), "#A02");

			// abandon should fail (since not invisible anymore)
			Assert.IsFalse(provider.Abandon(retrieved), "#A03");

			// list persisted message
			var key = provider.ListPersisted(storeName).Single();

			// get persisted message
			var persisted = provider.GetPersisted(storeName, key);
			Assert.IsTrue(persisted.HasValue, "#A04");
			Assert.IsTrue(persisted.Value.DataXml.HasValue, "#A05");

			// delete persisted message
			provider.DeletePersisted(storeName, key);

			Assert.AreEqual(0, blobProvider.List(OverflowingContainerName, QueueName).Count(), "#A06");

			// list no longer contains key
			Assert.IsFalse(provider.ListPersisted(storeName).Any(), "#A07");
		}

		[Test]
		public void QueueLatency()
		{
			var provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();
			Assert.IsNotNull(provider, "#A00");
			Assert.IsFalse(provider.GetApproximateLatency(QueueName).HasValue);

			provider.Put(QueueName, 100);
			var latency = provider.GetApproximateLatency(QueueName);
			Assert.IsTrue(latency.HasValue);
			Assert.IsTrue(latency.Value >= TimeSpan.Zero && latency.Value < TimeSpan.FromMinutes(10));

			provider.Delete(100);
		}
	}

	[Serializable]
	public struct MyStruct
	{
		public int IntegerValue;
		public string StringValue;
	}

	[DataContract]
	public class MyMessage
	{
		[DataMember(IsRequired = false)]
		public Guid MyGuid { get; private set; }

		[DataMember]
		public byte[] MyBuffer { get; set; }

		public MyMessage()
		{
			MyGuid = Guid.NewGuid();
		}
	}
}
