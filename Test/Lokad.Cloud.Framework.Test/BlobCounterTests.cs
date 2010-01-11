#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Azure.Test;
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
			//setting the provider
			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(ContainerName);
			
			//creating the threads
			const int threadsCount = 20;
			var threads = Enumerable.Range(0, threadsCount)
									.Select(i => new Thread(Increment))
									.ToArray();

			var random = new Random();
			var increments = Range.Array(threadsCount).Select(e => Range.Array(100).Select(i => random.Next(20) - 10).ToArray()).ToArray();
			int sum = increments.Sum(e => e.Sum());

			//creating thread parameters
			var counter = new BlobCounter(provider, ContainerName, "SomeBlobName");
			counter.Reset(0);

			Thread.Sleep(2000);

			var threadParameters = Enumerable.Range(0, threadsCount).Select(i =>
				new CounterParameters()
				{
					BlobCounter = new BlobCounter(provider, ContainerName, "SomeBlobName"),
					Increments = increments[i] 
				}).ToArray();

			//Starting threads
			Enumerable.Range(0, threadsCount).ForEach(i => threads[i].Start(threadParameters[i]));

			Thread.Sleep(2000);

			Assert.AreEqual(sum, counter.GetValue(), "values should be equal, BlobCounter supposed to be thread-safe");
		}

		static void Increment(object parameters)
		{
			if (parameters is CounterParameters)
			{
				var currentCounters = (CounterParameters)parameters;
				
				foreach (var increment in currentCounters.Increments)
				{
					currentCounters.BlobCounter.Increment(increment);
				}				
			}
		}

		class CounterParameters
		{
			public BlobCounter BlobCounter { get; set; }

			public int[] Increments { get; set; }
		}
	}
}
