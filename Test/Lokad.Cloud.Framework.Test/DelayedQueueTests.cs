#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud.Mock;
using NUnit.Framework;
using Lokad.Cloud.Azure.Test;
using System.Threading;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class DelayedQueueTests
	{
		string _testQueueName = null;

		[SetUp]
		public void SetUp()
		{
			_testQueueName = "q-" + Guid.NewGuid().ToString("N");
		}

		[Test]
		public void PutWithDelay()
		{
			// This test cannot run without the scheduler, so we have to check the
			// output container directly (thus this is not a black-box test)

			var blobStorage = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

			var delayer = new DelayedQueue(blobStorage);

			DateTime trigger = DateTime.Now.AddMinutes(10);

			delayer.PutWithDelay(21, trigger, _testQueueName);

			var msgName = new DelayedMessageName(trigger, Guid.Empty);
			var prefix = DelayedMessageName.GetPrefix(msgName, 1);

			var blobNames = blobStorage.List(prefix);

			Assert.AreEqual(1, blobNames.Count(), "Wrong blob count");
		}

		[Test]
		public void DelayedMessageAreRetrieved()
		{
			var blobStorage = new MemoryBlobStorageProvider();
			var queueStorage = new MemoryQueueStorageProvider(new CloudFormatter());
			var log = new MemoryLogger();
			var providers = new CloudInfrastructureProviders(blobStorage, queueStorage, log);
			var service = new FakeService() {Providers = providers};

			service.PutWithDelay(new FakeMessage(){SomeInt = 13, SomeDate = DateTime.Now}, DateTime.Now.Add(10.Seconds()));
			
			//Hack to get the number of 
			var name = TypeMapper.GetStorageName(typeof (FakeMessage));
			Assert.AreEqual(queueStorage.GetApproximateCount(name),0, "queue should be empty" );
			Thread.Sleep(20000);
			Assert.AreEqual(1, queueStorage.GetApproximateCount(name), "queue should not be empty");
		}

		class FakeService : QueueService<FakeMessage>
		{
			protected override void Start(FakeMessage message)
			{
				//do nothing
			}
		}

		class FakeMessage
		{
			public int SomeInt { get; set;}

			public DateTime SomeDate { get; set;}
		}
	}

}
