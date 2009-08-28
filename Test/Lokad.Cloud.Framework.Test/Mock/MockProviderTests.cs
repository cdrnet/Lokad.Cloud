#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

using Lokad.Cloud.Mock;

namespace Lokad.Cloud.Framework.Test.Mock
{
	[TestFixture]
	public class MockProviderTests
	{
		[Test]
		public void BlobsGetCreatedMonoThread()
		{
			const string containerName1 = "Container-1";
			const string containerName2 = "Container-2";
			const string containerName3 = "Container-3";
			const string blobPrefix = "mockBlobPrefix";
			const string secondBlobPrefix = "sndBlobPrefix";

			var storage = new MockStorageProvider();

			storage.CreateContainer(containerName1);
			storage.CreateContainer(containerName2);
			storage.CreateContainer(containerName3);

			storage.PutBlob(containerName1, blobPrefix + "/" + "blob1", new DateTime(2009,08,27));
			storage.PutBlob(containerName1, blobPrefix + "/" + "blob2", new DateTime(2009, 08, 28));
			storage.PutBlob(containerName1, blobPrefix + "/" + "blob3", new DateTime(2009, 08, 29));

			storage.PutBlob(containerName2, blobPrefix + "/" + "blob2", new DateTime(1984, 07, 06));

			storage.PutBlob(containerName1, secondBlobPrefix + "/" + "blob1", new DateTime(2009, 08, 30));

			var blobNames = storage.List(containerName1, blobPrefix);
			Assert.AreEqual(blobNames.Count(),3,"first container with first prefix does not hold 3 blobs");

			blobNames = storage.List(containerName2, blobPrefix);
			Assert.AreEqual(blobNames.Count(), 1, "second container with first prefix does not hold 1 blobs");

			blobNames = storage.List(containerName3, blobPrefix);
			Assert.AreEqual(blobNames.Count(), 0, "third container with first prefix does not hold 0 blob");

			blobNames = storage.List(containerName1, secondBlobPrefix);
			Assert.AreEqual(blobNames.Count(), 1, "first container with second prefix does not hold 1 blobs");

		}

		[Test]
		public void BlobsGetCreatedMultiThread()
		{
			const string containerNamePrefix = "Container-";
			
			const string blobPrefix = "mockBlobPrefix";

			var storage = new MockStorageProvider();

			storage.CreateContainer(containerNamePrefix+1);
			storage.CreateContainer(containerNamePrefix+2);


			var threads = Enumerable.Range(0, 32).Select(i=> new Thread(new ParameterizedThreadStart(AddValueToContainer))).ToArray();
			var threadParameters = Enumerable.Range(0, 32).Select(i => 
				i<=15 
				? new ThreadParameters("threadId" + i, "Container-1", storage)
				: new ThreadParameters("threadId" + i, "Container-2" , storage)).ToArray();

			Enumerable.Range(0,32).ForEach(i=> threads[i].Start(threadParameters[i]));
			
			Thread.Sleep(60000);

			var blobNames = storage.List("Container-1", blobPrefix);
			Assert.AreEqual(16000, blobNames.Count(), "first container with corresponding prefix does not hold 3 blobs");

			blobNames = storage.List("Container-2", blobPrefix);
			Assert.AreEqual(16000, blobNames.Count(), "second container with corresponding prefix does not hold 1 blobs");

		}

		public static void AddValueToContainer(object parameters)
		{
			if (parameters is ThreadParameters)
			{
				var castedParameters = (ThreadParameters)parameters;
				var random = new Random();
				for (int i = 0; i < 1000; i++)
				{
					castedParameters.Storage.PutBlob(castedParameters.ContainerName, "mockBlobPrefix" + castedParameters.ThreadId + "/blob" + i, random.NextDouble());
				}
			}
		}

		class ThreadParameters
		{
			public MockStorageProvider Storage { get; set; }
			public string ThreadId { get; set; }
			public string ContainerName { get; set; }

			public ThreadParameters(string threadId, string containerName, MockStorageProvider storage)
			{
				Storage = storage;
				ThreadId = threadId;
				ContainerName = containerName;
			}
		}
	}
}
