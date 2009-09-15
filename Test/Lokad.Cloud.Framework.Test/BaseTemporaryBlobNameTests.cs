using System;
using System.Runtime.Serialization.Formatters.Binary;
using Lokad.Cloud.Mock;
using Lokad.Quality;
using NUnit.Framework;

namespace Lokad.Cloud.Test
{
	class MyBlobName : BaseTemporaryBlobName<double>
	{
		public readonly string MyDefaulPrefix;

		public MyBlobName(DateTime expiration, string prefix)
			: base(expiration, prefix)
		{
			MyDefaulPrefix = DefaultPrefix;
		}
	}

	class Point
	{
		public double X { get; set; }

		public double Y { get; set; }

		public double Z { get; set; }
	}

	class AdvancedBlobName : BaseTemporaryBlobName<Point>
	{
		// caution: field order DOES matter.
		[UsedImplicitly] public readonly Guid AccountId;
		[UsedImplicitly] public readonly Guid ChunkId;
		[UsedImplicitly] public readonly int ChunkSize;

		public AdvancedBlobName(DateTime expiration, string prefix, Guid accountId, Guid chunkId, int chunkSize)
			: base(expiration, prefix)
		{
			AccountId = accountId;
			ChunkId = chunkId;
			ChunkSize = chunkSize;
		}

		/// <summary>Syntactic sugar with implicit <c>Operation</c> argument.</summary>
		public static AdvancedBlobName New(DateTime expiration, Guid accountId, Guid chunkId, int chunkSize)
		{
			return new AdvancedBlobName(expiration, null, accountId, chunkId, chunkSize);
		}

		/// <summary>Helper to list all points of a given account.</summary>
		public static BlobNamePrefix<AdvancedBlobName> GetPrefix(DateTime expiration, Guid accountId)
		{
			return GetPrefix(new AdvancedBlobName(expiration, null, accountId, Guid.Empty, 0), 3);
		}
	}

	[TestFixture]
	public class BaseTemporaryBlobNameTests
	{
		[Test]
		public void DefaultPrefix()
		{
			var name = new MyBlobName(DateTime.UtcNow, string.Empty);
			Assert.AreEqual("Lokad.Cloud.Test.MyBlobName", name.MyDefaulPrefix, "#A00");
		}

		[Test]
		public void AdvancedBlobNamePrefixTest()
		{
			var accountId = Guid.NewGuid();
			var chunkId = Guid.NewGuid();
			var mockProviders =
				new ProvidersForCloudStorage(new MemoryBlobStorageProvider(),
					new MemoryQueueStorageProvider(new BinaryFormatter()), new MemoryLogger());

			var blobStorage = mockProviders.BlobStorage;

			var point1 = new Point() {X = 2.0, Y = 1.2, Z = 3.4};
			var point2 = new Point() { X = 2.1, Y = 1.3, Z = 3.5 };
			var expirationDate = DateTime.Now + new TimeSpan(30, 0, 0, 0);
			var blobName = AdvancedBlobName.New(expirationDate, accountId, chunkId, 1);
			blobStorage.PutBlob(blobName, point1);
			blobStorage.PutBlob(blobName, point2);

			var allBlobsNameInBlobStorage = blobStorage.List(AdvancedBlobName.GetPrefix(expirationDate, accountId));
			//Assert.AreEqual(2, allBlobsNameInBlobStorage, "#A01 We should retrieve 2 blobs from the blobStorage.");
		}

	}
}
