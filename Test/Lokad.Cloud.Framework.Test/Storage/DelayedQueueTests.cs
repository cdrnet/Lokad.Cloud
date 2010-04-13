#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Test;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test
{
	[TestFixture]
	public class DelayedQueueTests
	{
		string _testQueueName;

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

			var trigger = DateTimeOffset.UtcNow.AddMinutes(5);

			delayer.PutWithDelay(21, trigger, _testQueueName);

			var prefix = new DelayedMessageReference(trigger, Guid.Empty);

			var blobReferences = blobStorage.List(prefix);

			Assert.AreEqual(1, blobReferences.Count(), "Wrong blob count");
		}
	}

}
