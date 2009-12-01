#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Linq;
using NUnit.Framework;
using Lokad.Cloud;
using System.Text;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Azure.Test
{
	[TestFixture]
	public class QueueStorageProviderTests
	{
		private const string BaseQueueName = "tests-queuestorageprovider-";
		private string QueueName;

		private static Random _rand = new Random();

		[SetUp]
		public void SetUp() {
			QueueName = BaseQueueName + Guid.NewGuid().ToString("N");
		}

		[TearDown]
		public void TearDown() {
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

			provider.Delete(QueueName, retrieved);
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

			for (int i = 0; i < message.MyBuffer.Length; i++ )
			{
				Assert.AreEqual(message.MyBuffer[i], retrieved.MyBuffer[i], "#A02-" + i);	
			}

			provider.Delete(QueueName, retrieved);
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

			for(int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testStruct);
			}

			var outStruct1 = provider.Get<MyStruct>(QueueName, 1).First();
			var outStruct2 = provider.Get<MyStruct>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(QueueName, outStruct1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(QueueName, outStruct2), "2nd Delete failed");
			Assert.IsFalse(provider.Delete(QueueName, outStruct2), "3nd Delete succeeded");

			var outAllStructs = provider.Get<MyStruct>(QueueName, 20);
			Assert.AreEqual(8, outAllStructs.Count(), "Wrong queue item count");
			foreach(var str in outAllStructs)
			{
				Assert.AreEqual(testStruct.IntegerValue, str.IntegerValue, "Wrong integer value");
				Assert.AreEqual(testStruct.StringValue, str.StringValue, "Wrong string value");
				Assert.IsTrue(provider.Delete(QueueName, str), "Delete failed");
			}

			var testDouble = 3.6D;

			for(int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testDouble);
			}

			var outDouble1 = provider.Get<double>(QueueName, 1).First();
			var outDouble2 = provider.Get<double>(QueueName, 1).First();
			var outDouble3 = provider.Get<double>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(QueueName, outDouble1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(QueueName, outDouble2), "2nd Delete failed");
			Assert.IsTrue(provider.Delete(QueueName, outDouble3), "3nd Delete failed");
			Assert.IsFalse(provider.Delete(QueueName, outDouble2), "3nd Delete succeeded");

			var outAllDoubles = provider.Get<double>(QueueName, 20);
			Assert.AreEqual(7, outAllDoubles.Count(), "Wrong queue item count");
			foreach(var dbl in outAllDoubles)
			{
				Assert.AreEqual(testDouble, dbl, "Wrong double value");
				Assert.IsTrue(provider.Delete(QueueName, dbl), "Delete failed");
			}

			var testString = "hi there!";

			for(int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testString);
			}

			var outString1 = provider.Get<string>(QueueName, 1).First();
			var outString2 = provider.Get<string>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(QueueName, outString1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(QueueName, outString2), "2nd Delete failed");
			Assert.IsFalse(provider.Delete(QueueName, outString2), "3nd Delete succeeded");

			var outAllStrings = provider.Get<string>(QueueName, 20);
			Assert.AreEqual(8, outAllStrings.Count(), "Wrong queue item count");
			foreach(var str in outAllStrings)
			{
				Assert.AreEqual(testString, str, "Wrong string value");
				Assert.IsTrue(provider.Delete(QueueName, str), "Delete failed");
			}

			var testClass = new StringBuilder("text");

			for(int i = 0; i < 10; i++)
			{
				provider.Put(QueueName, testClass);
			}

			var outClass1 = provider.Get<StringBuilder>(QueueName, 1).First();
			var outClass2 = provider.Get<StringBuilder>(QueueName, 1).First();
			Assert.IsTrue(provider.Delete(QueueName, outClass1), "1st Delete failed");
			Assert.IsTrue(provider.Delete(QueueName, outClass2), "2nd Delete failed");
			Assert.IsFalse(provider.Delete(QueueName, outClass2), "3nd Delete succeeded");

			var outAllClasses = provider.Get<StringBuilder>(QueueName, 20);
			Assert.AreEqual(8, outAllClasses.Count(), "Wrong queue item count");
			foreach(var cls in outAllClasses)
			{
				Assert.AreEqual(testClass.ToString(), cls.ToString(), "Wrong deserialized class value");
				Assert.IsTrue(provider.Delete(QueueName, cls), "Delete failed");
			}
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
