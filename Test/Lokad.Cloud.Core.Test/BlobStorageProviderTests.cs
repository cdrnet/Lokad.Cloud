#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using NUnit.Framework;

namespace Lokad.Cloud.Core.Test
{
	[TestFixture]
	public class BlobStorageProviderTests
	{
		private const string ContainerName = "tests-blobstorageprovider-mycontainer";
		private const string BlobName = "myprefix/myblob";

		[Test]
		public void CreatePutGetDelete()
		{
			IBlobStorageProvider provider = GlobalSetup.Container.Resolve<BlobStorageProvider>();

			provider.CreateContainer(ContainerName);

			var blob = new MyBlob();
			provider.PutBlob(ContainerName, BlobName, blob);

			var retrievedBlob = provider.GetBlob<MyBlob>(ContainerName, BlobName);

			Assert.AreEqual(blob.MyGuid, retrievedBlob.MyGuid, "#A01");

			Assert.IsTrue(provider.List(ContainerName, "myprefix").Contains(BlobName), "#A02");
			Assert.IsTrue(!provider.List(ContainerName, "notmyprefix").Contains(BlobName), "#A03");

			Assert.IsTrue(provider.DeleteBlob(ContainerName, BlobName), "#A04");

			Assert.IsTrue(provider.DeleteContainer(ContainerName), "#A05");
		}

		[Test]
		public void UpdateIfNotModified()
		{
			IBlobStorageProvider provider = GlobalSetup.Container.Resolve<BlobStorageProvider>();

			provider.CreateContainer(ContainerName);

			provider.PutBlob(ContainerName, BlobName, 1);
			var isUpdated = provider.UpdateIfNotModified<int>(ContainerName, BlobName, i => i + 1);

			Assert.IsTrue(isUpdated, "#A00");

			var val = provider.GetBlob<int>(ContainerName, BlobName);
            Assert.AreEqual(2, val, "#A01");

			Assert.IsTrue(provider.DeleteContainer(ContainerName), "#A02");
		}
	}

	[Serializable]
	public class MyBlob
	{
		public Guid MyGuid { get; private set; }

		public MyBlob()
		{
			MyGuid = new Guid();
		}
	}
}
