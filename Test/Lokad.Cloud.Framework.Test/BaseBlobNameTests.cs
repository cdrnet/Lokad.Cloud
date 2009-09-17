#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using NUnit.Framework;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class BaseBlobNameTests
	{
		// ReSharper disable InconsistentNaming

		class PatternA : BaseBlobName
		{
			// not a field
			public override string ContainerName { get { return "my-test-container"; } }

			[Rank(0)] public readonly DateTime Timestamp;
			[Rank(1)] public readonly long AccountHRID;
			[Rank(2)] public readonly Guid ChunkID;
			[Rank(3)] public readonly int ChunkSize;

			public PatternA(DateTime timestamp, long accountHrid, Guid chunkID, int chunkSize)
			{
				Timestamp = timestamp;
				AccountHRID = accountHrid;
				ChunkID = chunkID;
				ChunkSize = chunkSize;
			}
		}

		class PatternB : BaseBlobName
		{
			// not a field
			public override string ContainerName { get { return "my-test-container"; } }

			[Rank(0)] public readonly long AccountHRID;
			[Rank(1)] public readonly Guid ChunkID;

			public PatternB(Guid chunkID, long accountHrid)
			{
				ChunkID = chunkID;
				AccountHRID = accountHrid;
			}
		}

		class PatternC : BaseBlobName
		{
			// not a field
			public override string ContainerName { get { return "my-test-container"; } }

			[Rank(0)] public readonly Guid ChunkID;
			[Rank(1)] public readonly long AccountId;
			
			public PatternC(Guid chunkID, long accountId)
			{
				ChunkID = chunkID;
				AccountId = accountId;
			}
		}

		class PatternD : PatternC
		{
			// position should always respect inheritance
			[Rank(0)] public readonly long UserId;

			public PatternD(Guid chunkID, long accountId, long userId) : base(chunkID, accountId)
			{
				UserId = userId;
			}
		}


		[Test]
		public void Conversion_round_trip()
		{
			var date = new DateTime(2009, 1, 1, 3, 4, 5);
			var original = new PatternA(date, 12000, Guid.NewGuid(), 120);

			var name = BaseBlobName.Print(original);

			Console.WriteLine(name);

			var parsed = BaseBlobName.Parse<PatternA>(name);
			Assert.AreNotSame(original, parsed);
			Assert.AreEqual(original.Timestamp, parsed.Timestamp);
			Assert.AreEqual(original.AccountHRID, parsed.AccountHRID);
			Assert.AreEqual(original.ChunkID, parsed.ChunkID);
			Assert.AreEqual(original.ChunkSize, parsed.ChunkSize);
		}

		[Test]
		public void Get_ContainerName()
		{
			var name = BaseBlobName.GetContainerName<PatternA>();
			Assert.AreEqual("my-test-container", name);
		}

		[Test]
		public void Two_Patterns()
		{
			// actually ensures that the implementation supports two patterns

			var date = new DateTime(2009, 1, 1, 3, 4, 5);
			var pa = new PatternA(date, 12000, Guid.NewGuid(), 120);
			var pb = new PatternB(Guid.NewGuid(), 1000);

			Assert.AreNotEqual(pa.ToString(), pb.ToString());
		}

		[Test]
		public void Field_Order_Matters()
		{
			var g = Guid.NewGuid();
			var pb = new PatternB(g, 1000);
			var pc = new PatternC(g, 1000);

			Assert.AreNotEqual(pb.ToString(), pc.ToString());	
		}

		[Test]
		public void Field_Order_Works_With_Inheritance()
		{
			var g = Guid.NewGuid();
			var pc = new PatternC(g, 1000);
			var pd = new PatternD(g, 1000, 1234);

			Assert.IsTrue(pd.ToString().StartsWith(pc.ToString()));
		}

		[Test, ExpectedException(typeof(ArgumentException))]
		public void Wrong_type_is_detected()
		{
			var original = new PatternB(Guid.NewGuid(), 1000);
			var name = BaseBlobName.Print(original);
			BaseBlobName.Parse<PatternA>(name);
		}

		[Test]
		public void PartialPrint()
		{
			var date = new DateTime(2009, 1, 1, 3, 4, 5);
			var pattern = new PatternA(date, 12000, Guid.NewGuid(), 120);

			Assert.IsTrue(pattern.ToString().StartsWith(BaseBlobName.PartialPrint(pattern, 1)));
			Assert.IsTrue(pattern.ToString().StartsWith(BaseBlobName.PartialPrint(pattern, 2)));
			Assert.IsTrue(pattern.ToString().StartsWith(BaseBlobName.PartialPrint(pattern, 3)));
		}
	}
}
