#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud;
using NUnit.Framework;

// TODO: refactor tests so that containers do not have to be created each time.

namespace Lokad.Cloud.Azure.Test
{
	[TestFixture]
	public class BlobStorageProviderTests
	{
		private const string ContainerName = "tests-blobstorageprovider-mycontainer";
		private const string BlobName = "myprefix/myblob";

		[Test]
		public void CreatePutGetDelete()
		{
			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(ContainerName);

			var blob = new MyBlob();
			provider.PutBlob(ContainerName, BlobName, blob);

			var retrievedBlob = provider.GetBlob<MyBlob>(ContainerName, BlobName);

			Assert.AreEqual(blob.MyGuid, retrievedBlob.MyGuid, "#A01");
			Assert.IsTrue(provider.List(ContainerName, "myprefix").Contains(BlobName), "#A02");
			Assert.IsTrue(!provider.List(ContainerName, "notmyprefix").Contains(BlobName), "#A03");

			// testing ETag
			provider.DeleteBlob(ContainerName, BlobName);
			var etag = provider.GetBlobEtag(ContainerName, BlobName);
			Assert.IsNull(etag, "Deleted blob has no etag.");

			provider.PutBlob(ContainerName, BlobName, 1);
			etag = provider.GetBlobEtag(ContainerName, BlobName);
			Assert.IsNotNull(etag, "Blob should have etag.");

			var newEtag = provider.GetBlobEtag(ContainerName, BlobName);
			Assert.AreEqual(etag, newEtag, "Etag should be unchanged.");

			// Verify that overwrite flag works as expected
			Assert.IsFalse(provider.PutBlob(ContainerName, BlobName, blob, false), "Blob should not be overwritten");
			Assert.IsTrue(provider.PutBlob(ContainerName, BlobName, blob, true), "Blob should be overwritten");
			string newEtagOut = provider.GetBlobEtag(ContainerName, BlobName);
			Assert.AreNotEqual(newEtag, newEtagOut, "Etag should be changed");
			newEtag = newEtagOut;

			// Test that blob is not retrieved because it is unchanged
			newEtagOut = "dummy";
			MyBlob output = provider.GetBlobIfModified<MyBlob>(ContainerName, BlobName, newEtag, out newEtagOut);
			Assert.IsNull(newEtagOut, "Etag should be null because blob is unchanged");
			Assert.IsNull(output, "Retrieved blob should be null because it is unchanged");

			provider.PutBlob(ContainerName, BlobName, 2);
			newEtag = provider.GetBlobEtag(ContainerName, BlobName);
			Assert.AreNotEqual(etag, newEtag, "Etag should be changed.");

			// Test that blob is retrieved because it is changed
			string myPreviousEtag = newEtagOut;
			newEtagOut = "dummy";
			int outputInt = provider.GetBlobIfModified<int>(ContainerName, BlobName, myPreviousEtag, out newEtagOut);
			Assert.AreNotEqual(myPreviousEtag, newEtagOut, "Etag should be updated");
			Assert.AreEqual(2, outputInt, "Wrong blob content");

			// testing UpdateIfNotModified
			provider.PutBlob(ContainerName, BlobName, 1);
			int ignored;
			var isUpdated = provider.UpdateIfNotModified(ContainerName, BlobName, i => i + 1, out ignored);

			Assert.IsTrue(isUpdated, "#A00");

			var val = provider.GetBlob<int>(ContainerName, BlobName);
			Assert.AreEqual(2, val, "#A01");

			// PutBlob with etag out parameter
			newEtagOut = "dummy";
			bool isSaved = provider.PutBlob(ContainerName, BlobName, 6, false, out newEtagOut);
			Assert.IsFalse(isSaved, "Blob should not have been overwritten");
			Assert.IsNull(newEtagOut, "Etag should be null");

			newEtagOut = "dummy";
			isSaved = provider.PutBlob(ContainerName, BlobName, 7, true, out newEtagOut);
			Assert.IsTrue(isSaved, "Blob should have been overwritten");
			Assert.IsNotNull(newEtagOut, "Etag should be changed");

			Assert.AreEqual(7, provider.GetBlob<int>(ContainerName, BlobName), "Blob was not correctly saved");

			// cleanup
			Assert.IsTrue(provider.DeleteBlob(ContainerName, BlobName), "#A04");
			Assert.IsTrue(provider.DeleteContainer(ContainerName), "#A05");
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
