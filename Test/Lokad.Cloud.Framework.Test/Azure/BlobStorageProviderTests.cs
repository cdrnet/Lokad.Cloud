#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud;
using NUnit.Framework;
using System.Runtime.Serialization;

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

			Assert.IsTrue(retrievedBlob.HasValue, "#A01");
			Assert.AreEqual(blob.MyGuid, retrievedBlob.Value.MyGuid, "#A02");
			Assert.IsTrue(provider.List(ContainerName, "myprefix").Contains(BlobName), "#A03");
			Assert.IsTrue(!provider.List(ContainerName, "notmyprefix").Contains(BlobName), "#A04");

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
			Maybe<MyBlob> output = provider.GetBlobIfModified<MyBlob>(ContainerName, BlobName, newEtag, out newEtagOut);
			Assert.IsNull(newEtagOut, "Etag should be null because blob is unchanged");
			Assert.IsFalse(output.HasValue, "Retrieved blob should be null because it is unchanged");

			provider.PutBlob(ContainerName, BlobName, 2);
			newEtag = provider.GetBlobEtag(ContainerName, BlobName);
			Assert.AreNotEqual(etag, newEtag, "Etag should be changed.");

			// Test that blob is retrieved because it is changed
			string myPreviousEtag = newEtagOut;
			newEtagOut = "dummy";
			Maybe<int> outputInt = provider.GetBlobIfModified<int>(ContainerName, BlobName, myPreviousEtag, out newEtagOut);
			Assert.AreNotEqual(myPreviousEtag, newEtagOut, "Etag should be updated");
			Assert.AreEqual(2, outputInt.Value, "Wrong blob content");

			// testing UpdateIfNotModified
			provider.PutBlob(ContainerName, BlobName, 1);
			int ignored;
			var isUpdated = provider.UpdateIfNotModified(ContainerName, BlobName, i => i.HasValue ? i.Value + 1 : 1, out ignored);

			Assert.IsTrue(isUpdated, "#A00");

			var val = provider.GetBlob<int>(ContainerName, BlobName);
			Assert.AreEqual(2, val.Value, "#A01");

			// PutBlob with etag out parameter
			newEtagOut = "dummy";
			bool isSaved = provider.PutBlob(ContainerName, BlobName, 6, false, out newEtagOut);
			Assert.IsFalse(isSaved, "Blob should not have been overwritten");
			Assert.IsNull(newEtagOut, "Etag should be null");

			newEtagOut = "dummy";
			isSaved = provider.PutBlob(ContainerName, BlobName, 7, true, out newEtagOut);
			Assert.IsTrue(isSaved, "Blob should have been overwritten");
			Assert.IsNotNull(newEtagOut, "Etag should be changed");

			Assert.IsTrue(provider.GetBlob<int>(ContainerName, BlobName).HasValue, "Blob was not correctly saved.1");
			Assert.AreEqual(7, provider.GetBlob<int>(ContainerName, BlobName).Value, "Blob was not correctly saved.2");

			// cleanup
			Assert.IsTrue(provider.DeleteBlob(ContainerName, BlobName), "#A04");
			Assert.IsTrue(provider.DeleteContainer(ContainerName), "#A05");
		}

		[Test]
		public void CreatePutGetRangeDelete()
		{
			var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(privateContainerName);

			var blobNames = new string[]
			{
				BlobName + "-0",
				BlobName + "-1",
				BlobName + "-2",
				BlobName + "-3"
			};

			var inputBlobs = new MyBlob[]
			{
				new MyBlob(),
				new MyBlob(),
				new MyBlob(),
				new MyBlob()
			};

			for(int i = 0; i < blobNames.Length; i++)
			{
				provider.PutBlob(privateContainerName, blobNames[i], inputBlobs[i]);
			}

			string[] allEtags;
			var allBlobs = provider.GetBlobRange<MyBlob>(privateContainerName, blobNames, out allEtags);

			Assert.AreEqual(blobNames.Length, allEtags.Length, "Wrong etags array length");
			Assert.AreEqual(blobNames.Length, allBlobs.Length, "Wrong blobs array length");

			for(int i = 0; i < allBlobs.Length; i++)
			{
				Assert.IsNotNull(allEtags[i], "Etag should have been set");
				Assert.IsTrue(allBlobs[i].HasValue, "Blob should have content");
				Assert.AreEqual(inputBlobs[i].MyGuid, allBlobs[i].Value.MyGuid, "Wrong blob content");
			}

			// Test missing blob
			var wrongBlobNames = new string[blobNames.Length + 1];
			Array.Copy(blobNames, wrongBlobNames, blobNames.Length);
			wrongBlobNames[wrongBlobNames.Length - 1] = "inexistent-blob";

			allBlobs = provider.GetBlobRange<MyBlob>(privateContainerName, wrongBlobNames, out allEtags);

			Assert.AreEqual(wrongBlobNames.Length, allEtags.Length, "Wrong etags array length");
			Assert.AreEqual(wrongBlobNames.Length, allBlobs.Length, "Wrong blobs array length");

			for(int i = 0; i < allBlobs.Length - 1; i++)
			{
				Assert.IsNotNull(allEtags[i], "Etag should have been set");
				Assert.IsTrue(allBlobs[i].HasValue, "Blob should have content");
				Assert.AreEqual(inputBlobs[i].MyGuid, allBlobs[i].Value.MyGuid, "Wrong blob content");
			}
			Assert.IsNull(allEtags[allEtags.Length - 1], "Etag should be null");
			Assert.IsFalse(allBlobs[allBlobs.Length - 1].HasValue, "Blob should not have a value");

			provider.DeleteContainer(privateContainerName);
		}

		[Test]
		public void TransientType()
		{
			var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(privateContainerName);

			var item1 = new Transient2() { Value2 = 100 };
			provider.PutBlob(privateContainerName, "test", item1);
			var item1Out = provider.GetBlob<Transient2>(privateContainerName, "test");
			Assert.AreEqual(item1.Value2, item1Out.Value.Value2);

			var item2 = provider.GetBlob<Transient3>(privateContainerName, "test");
			Assert.IsFalse(item2.HasValue);

			provider.DeleteContainer(privateContainerName);
		}

		[Test]
		public void TypeMismatch()
		{
			var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(privateContainerName);

			provider.PutBlob(privateContainerName, "test", new Transient1() { Value = 10 });

			Assert.Throws<InvalidOperationException>(() => provider.GetBlob<string>(privateContainerName, "test"));

			provider.PutBlob(privateContainerName, "test", "string content");

			provider.DeleteContainer(privateContainerName);
		}

		[Test]
		public void NullableType_Default()
		{
			var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(privateContainerName);

			int? value1 = 10;
			int? value2 = null;

			provider.PutBlob(privateContainerName, "test1", value1);
			provider.PutBlob(privateContainerName, "test2", value1);

			var output1 = provider.GetBlob<int?>(privateContainerName, "test1");
			var output2 = provider.GetBlob<int?>(privateContainerName, "test2");

			Assert.AreEqual(value1.Value, output1.Value);
			Assert.IsFalse(value2.HasValue);

			provider.DeleteContainer(privateContainerName);
		}

		[Test]
		public void NullableType_Transient_Struct()
		{
			var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(privateContainerName);

			MyNullableStruct? value1 = new MyNullableStruct() { MyValue = 10 };
			MyNullableStruct? value2 = null;

			provider.PutBlob(privateContainerName, "test1", value1);
			provider.PutBlob(privateContainerName, "test2", value1);

			var output1 = provider.GetBlob<MyNullableStruct?>(privateContainerName, "test1");
			var output2 = provider.GetBlob<MyNullableStruct?>(privateContainerName, "test2");

			Assert.AreEqual(value1.Value, output1.Value);
			Assert.IsFalse(value2.HasValue);

			provider.DeleteContainer(privateContainerName);
		}

	}

	[DataContract]
	[Transient]
	internal struct MyNullableStruct
	{
		[DataMember]
		public int MyValue;
	}

	[DataContract]
	[Transient(false)]
	internal class Transient1
	{
		[DataMember]
		public int Value;
	}

	[DataContract]
	[Transient(false)]
	internal class Transient2
	{
		[DataMember]
		public int Value2;
	}

	[DataContract]
	[Transient(false)]
	internal class Transient3
	{
		[DataMember]
		public int Value;
	}

	[Serializable]
	internal class MyBlob
	{
		public Guid MyGuid { get; private set; }

		public MyBlob()
		{
			MyGuid = new Guid();
		}
	}
}
