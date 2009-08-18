#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Azure.Test;
using Lokad.Cloud.Core;
using NUnit.Framework;

namespace Lokad.Cloud.Framework.Test
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
	}
}
