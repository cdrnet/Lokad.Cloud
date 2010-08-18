#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Test;
using Lokad.Threading;
using NUnit.Framework;

// TODO: refactor tests so that containers do not have to be created each time.

namespace Lokad.Cloud.Storage.Test
{
	[TestFixture]
	public class BlobStorageProviderTests
	{
		private const string ContainerName = "tests-blobstorageprovider-mycontainer";
		private const string BlobName = "myprefix/myblob";

		readonly IBlobStorageProvider Provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		[TestFixtureSetUp]
		public void Setup()
		{
			Provider.CreateContainer(ContainerName);
			Provider.DeleteBlob(ContainerName, BlobName);
		}

		[Test]
		public void GetAndDelete()
		{
			Provider.DeleteBlob(ContainerName, BlobName);
			Assert.IsFalse(Provider.GetBlob<int>(ContainerName, BlobName).HasValue, "#A00");
		}

		[Test]
		public void BlobHasEtag()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);
			var etag = Provider.GetBlobEtag(ContainerName, BlobName);
			Assert.IsNotNull(etag, "#A00");
		}

		[Test]
		public void MissingBlobHasNoEtag()
		{
			Provider.DeleteBlob(ContainerName, BlobName);
			var etag = Provider.GetBlobEtag(ContainerName, BlobName);
			Assert.IsNull(etag, "#A00");
		}

		[Test]
		public void PutBlobEnforceNoOverwrite()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);

			string etag;
			var isSaved = Provider.PutBlob(ContainerName, BlobName, 6, false, out etag);
			Assert.IsFalse(isSaved, "#A00");
			Assert.IsNull(etag, "#A01");

			Assert.IsTrue(Provider.GetBlob<int>(ContainerName, BlobName).HasValue, "#A02");
			Assert.AreEqual(1, Provider.GetBlob<int>(ContainerName, BlobName).Value, "#A03");
		}

		[Test]
		public void PutBlobEnforceOverwrite()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);

			string etag;
			var isSaved = Provider.PutBlob(ContainerName, BlobName, 6, true, out etag);
			Assert.IsTrue(isSaved, "#A00");
			Assert.IsNotNull(etag, "#A01");

			Assert.IsTrue(Provider.GetBlob<int>(ContainerName, BlobName).HasValue, "#A02");
			Assert.AreEqual(6, Provider.GetBlob<int>(ContainerName, BlobName).Value, "#A03");
		}

        [Test]
        public void PutBlobEnforceMatchingEtag()
        {
            Provider.PutBlob(ContainerName, BlobName, 1);

            var etag = Provider.GetBlobEtag(ContainerName, BlobName);
            var isUpdated = Provider.PutBlob(ContainerName, BlobName, 2, Guid.NewGuid().ToString());

            Assert.IsTrue(!isUpdated, "#A00 Blob shouldn't be updated if etag is not matching");

            isUpdated = Provider.PutBlob(ContainerName, BlobName, 3, etag);
            Assert.IsTrue(isUpdated, "#A01 Blob should have been updated");
        }

		[Test]
		public void EtagChangesOnlyWithBlogChange()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);
			var etag = Provider.GetBlobEtag(ContainerName, BlobName);
			var newEtag = Provider.GetBlobEtag(ContainerName, BlobName);
			Assert.AreEqual(etag, newEtag, "#A00");
		}

		[Test]
		public void EtagChangesWithBlogChange()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);
			var etag = Provider.GetBlobEtag(ContainerName, BlobName);
			Provider.PutBlob(ContainerName, BlobName, 1);
			var newEtag = Provider.GetBlobEtag(ContainerName, BlobName);
			Assert.AreNotEqual(etag, newEtag, "#A00.");
		}

		[Test]
		public void GetBlobIfNotModifiedNoChangeNoRetrieval()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);
			var etag = Provider.GetBlobEtag(ContainerName, BlobName);

			string newEtag;
			var output = Provider.GetBlobIfModified<MyBlob>(ContainerName, BlobName, etag, out newEtag);

			Assert.IsNull(newEtag, "#A00");
			Assert.IsFalse(output.HasValue, "#A01");
		}

		[Test]
		public void GetBlobIfNotModifiedWithTypeMistmatch()
		{
			Provider.PutBlob(ContainerName, BlobName, 1); // pushing Int32

			try
			{
				string newEtag; // pulling MyBlob
				var output = Provider.GetBlobIfModified<MyBlob>(ContainerName, BlobName, "dummy", out newEtag);
				Assert.Fail("#A00");
			}
			catch (InvalidCastException)
			{
				// expected condition
			}
		}

		/// <summary>This test does not check the behavior of 'UpdateIfNotModified'
		/// in case of concurrency stress.</summary>
		[Test]
		public void UpdateIfNotModifiedNoStress()
		{
			Provider.PutBlob(ContainerName, BlobName, 1);
			int ignored;
			var isUpdated = Provider.UpdateIfNotModified(ContainerName, 
				BlobName, i => i.HasValue ? i.Value + 1 : 1, out ignored);
			Assert.IsTrue(isUpdated, "#A00");

			var val = Provider.GetBlob<int>(ContainerName, BlobName);
			Assert.AreEqual(2, val.Value, "#A01");
		}

		/// <summary>Loose check of the behavior of 'UpdateIfNotModified'
		/// under concurrency stress.</summary>
		[Test]
		public void UpdateIfNotModifiedWithStress()
		{
			Provider.PutBlob(ContainerName, BlobName, 0);

			var array = new bool[8];

			int ignored;

			array = array.SelectInParallel(
				k => Provider.UpdateIfNotModified(ContainerName,
					BlobName, i => i.HasValue ? i.Value + 1 : 1, out ignored), array.Length);

			Assert.IsTrue(array.Any(x => x), "#A00 write should have happened at least once.");
			Assert.IsTrue(array.Any(x => !x), "#A01 conflict should have happened at least once.");

			var count = Provider.GetBlob<int>(ContainerName, BlobName).Value;

			Assert.AreEqual(count, array.Count(x => x), "#A02 number of writes should match counter value.");
		}

		// TODO: CreatePutGetRangeDelete is way to complex as a unit test

		[Test]
		public void CreatePutGetRangeDelete()
		{
			var privateContainerName = "test-" + Guid.NewGuid().ToString("N");

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			provider.CreateContainer(privateContainerName);

			var blobNames = new[]
			{
				BlobName + "-0",
				BlobName + "-1",
				BlobName + "-2",
				BlobName + "-3"
			};

			var inputBlobs = new[]
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
		public void List()
		{
			var prefix = Guid.NewGuid().ToString("N");

			var prefixed = Range.Array(10).Convert(i => prefix + Guid.NewGuid().ToString("N"));
			var unprefixed = Range.Array(13).Convert(i => Guid.NewGuid().ToString("N"));

			foreach (var n in prefixed)
			{
				Provider.PutBlob(ContainerName, n, n);
			}

			foreach (var n in unprefixed)
			{
				Provider.PutBlob(ContainerName, n, n);
			}

			var list = Provider.List(ContainerName, prefix).ToArray();

			Assert.AreEqual(prefixed.Length, list.Length, "#A00");

			foreach(var n in list)
			{
				Assert.IsTrue(prefixed.Contains(n), "#A01");
				Assert.IsFalse(unprefixed.Contains(n), "#A02");
			}
		}

		[Test]
		public void GetBlobXml()
		{
			var data = new MyBlob();
			Provider.PutBlob(ContainerName, BlobName, data, true);

			string ignored;
			var blob = Provider.GetBlobXml(ContainerName, BlobName, out ignored);
			Provider.DeleteBlob(ContainerName, BlobName);

			Assert.IsTrue(blob.HasValue);
			var xml = blob.Value;
			var property = xml.Elements().Single();
			Assert.AreEqual(data.MyGuid, new Guid(property.Value));
		}
	}

	[Serializable]
	internal class MyBlob
	{
		public Guid MyGuid { get; private set; }

		public MyBlob()
		{
			MyGuid = Guid.NewGuid();
		}
	}
}
