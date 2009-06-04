#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using NUnit.Framework;

namespace Lokad.Cloud.Core.Test
{
	[TestFixture]
	public class BlobStorageProviderTests
	{
		private const string ContainerName = "tests-blobstorageprovider-mycontainer";
		private const string BlobName = "tests-blobstorageprovider-myblob";

		[Test]
		public void CreatePutGetDelete()
		{
			var provider = GlobalSetup.Container.Resolve<BlobStorageProvider>();

			provider.CreateContainer(ContainerName);

			var blob = new MyBlob();
			provider.PutBlob(ContainerName, BlobName, blob);

			var retrievedBlob = provider.GetBlob<MyBlob>(ContainerName, BlobName);

			Assert.AreEqual(blob.MyGuid, retrievedBlob.MyGuid, "#A01");

			Assert.IsTrue(provider.DeleteBlob(ContainerName, BlobName), "#A02");

			Assert.IsTrue(provider.DeleteContainer(ContainerName), "#A03");
		}
	}

	[Serializable]
	public class MyBlob
	{
		public Guid MyGuid { get; set; }

		public MyBlob()
		{
			MyGuid = new Guid();
		}
	}
}
