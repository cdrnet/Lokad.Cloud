#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Azure.Test;
using Lokad.Threading;
using NUnit.Framework;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class BlobCounterTests
	{
		private const string ContainerName = "tests-blobcounter-mycontainer";
		private const string BlobName = "myprefix/myblob";

		[Test]
		public void GetValueIncrement()
		{
			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(ContainerName);

			var counter = new BlobCounter(provider, ContainerName, BlobName);

			var val = (int)counter.GetValue();

			if (0 != val) counter.Delete();

			counter.Increment(10);
			val = (int) counter.GetValue();
			Assert.AreEqual(10, val, "#A00");

			var val2 = counter.Increment(-5);
			val = (int)counter.GetValue();
			Assert.AreEqual(5, val, "#A01");
			Assert.AreEqual(val, val2, "#A02");

			var flag1 = counter.Delete();
			var flag2 = counter.Delete();

			Assert.IsTrue(flag1, "#A03");
			Assert.IsFalse(flag2, "#A04");
		}

		[Test]
		public void IncrementMultiThread()
		{
			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(ContainerName);

			//creating thread parameters
			var count = new BlobCounter(provider, ContainerName, "SomeBlobName");
			count.Reset(0);

			var random = new Random();
			const int threadsCount = 4;
			var increments = Range.Array(threadsCount).Convert(e => Range.Array(5).Convert(i => random.Next(20) - 10));
			var localSums = increments.SelectInParallel(e =>
				{
					var counter = new BlobCounter(provider, ContainerName, "SomeBlobName");
					foreach (var increment in e)
					{
						counter.Increment(increment);
					}
					return e.Sum();
				}, threadsCount);

			Assert.AreEqual(localSums.Sum(), count.GetValue(), "values should be equal, BlobCounter supposed to be thread-safe");
		}
	}
}
