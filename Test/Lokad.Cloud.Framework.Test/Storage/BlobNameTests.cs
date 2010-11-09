#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.Storage.Blobs;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test
{
	[TestFixture]
	public class BlobNameTests
	{
		// ReSharper disable InconsistentNaming

		class PatternA : BlobName<string>
		{
			// not a field
			public override string ContainerName { get { return "my-test-container"; } }

			[Rank(0, true)] public readonly DateTime Timestamp;
			[Rank(1)] public readonly long AccountHRID;
			[Rank(2, true)] public readonly Guid ChunkID;
			[Rank(3)] public readonly int ChunkSize;

			public PatternA()
			{
			}

			public PatternA(DateTime timestamp, long accountHrid, Guid chunkID, int chunkSize)
			{
				Timestamp = timestamp;
				AccountHRID = accountHrid;
				ChunkID = chunkID;
				ChunkSize = chunkSize;
			}
		}

		class PatternB : BlobName<string>
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

		class PatternC : BlobName<string>
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

		class PatternE : BlobName<string>
		{
			// not a field
			public override string ContainerName { get { return "my-test-container"; } }

			[Rank(0)]
			public readonly DateTime UserTime;
			[Rank(1)]
			public readonly DateTimeOffset AbsoluteTime;

			// 'dummy' is not used on purpose
			// Provided to make sure 'BlobName' does not rely on specific constructor
			public PatternE(DateTime userTime, DateTimeOffset absoluteTime, string dummy)
			{
				UserTime = userTime;
				AbsoluteTime = absoluteTime;
			}
		}

		class PatternF : BlobName<string>
		{
			public override string ContainerName { get { return "my-test-container"; } }

			[Rank(0)] public long AccountHRID { get; set;}
			[Rank(1)] public Guid ChunkID { get; set; }
		}


		[Test]
		public void Conversion_round_trip()
		{
			var date = new DateTime(2009, 1, 1, 3, 4, 5);
			var original = new PatternA(date, 12000, Guid.NewGuid(), 120);

			var name = UntypedBlobName.Print(original);

			//Console.WriteLine(name);

			var parsed = UntypedBlobName.Parse<PatternA>(name);
			Assert.AreNotSame(original, parsed);
			Assert.AreEqual(original.Timestamp, parsed.Timestamp);
			Assert.AreEqual(original.AccountHRID, parsed.AccountHRID);
			Assert.AreEqual(original.ChunkID, parsed.ChunkID);
			Assert.AreEqual(original.ChunkSize, parsed.ChunkSize);
		}

		[Test]
		public void Get_ContainerName()
		{
			var name = UntypedBlobName.GetContainerName<PatternA>();
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

		[Test]
		public void Wrong_type_is_detected()
		{
			try
			{
				var original = new PatternB(Guid.NewGuid(), 1000);
				var name = UntypedBlobName.Print(original);
				UntypedBlobName.Parse<PatternA>(name);

				Assert.Fail("#A00");
			}
			catch (ArgumentException) {}
		}

		[Test]
		public void PartialPrint_Manage_EmptyGuid()
		{
			var date = new DateTime(2009, 1, 1, 3, 4, 5);
			var pattern = new PatternA(date, 12000, Guid.Empty, 120);
			Assert.IsTrue(!pattern.ToString().EndsWith(120.ToString()));
		}

		[Test]
		public void PartialPrint_Manage_DefaultTimeValue()
		{
			var pattern = new PatternA();
			Assert.AreEqual(string.Empty, pattern.ToString());
		}

		[Test]
		public void Time_zone_safe_when_using_DateTimeOffset()
		{
			var localOffset = TimeSpan.FromHours(-2);
			var now = DateTimeOffset.Now;
			// round to milliseconds, our time resolution in blob names
			now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, now.Offset);
			var unsafeNow = now.DateTime;

			var utcNow = now.ToUniversalTime();
			var localNow = now.ToOffset(localOffset);
			var unsafeUtcNow = utcNow.UtcDateTime;
			var unsafeLocalNow = localNow.DateTime;

			// 'dummy' argument set as 'null'
			var localString = UntypedBlobName.Print(new PatternE(unsafeLocalNow, localNow, null));
			var localName = UntypedBlobName.Parse<PatternE>(localString);

			Assert.AreEqual(now, localName.AbsoluteTime, "DateTimeOffset-local");
			Assert.AreEqual(utcNow, localName.AbsoluteTime, "DateTimeOffset-local-utc");
			Assert.AreEqual(localNow, localName.AbsoluteTime, "DateTimeOffset-local-local");

			Assert.AreNotEqual(unsafeNow, localName.UserTime, "DateTime-local");
			Assert.AreNotEqual(unsafeUtcNow, localName.UserTime, "DateTime-local-utc");
			Assert.AreEqual(unsafeLocalNow, localName.UserTime, "DateTime-local-local");

			Assert.AreNotEqual(unsafeUtcNow, localName.UserTime, "DateTime-local");

			// 'dummy' argument set as 'null'
			var utcString = UntypedBlobName.Print(new PatternE(unsafeUtcNow, utcNow, null));
			var utcName = UntypedBlobName.Parse<PatternE>(utcString);

			Assert.AreEqual(now, utcName.AbsoluteTime, "DateTimeOffset-utc");
			Assert.AreEqual(utcNow, utcName.AbsoluteTime, "DateTimeOffset-local-utc");
			Assert.AreEqual(localNow, utcName.AbsoluteTime, "DateTimeOffset-local-local");

			if (unsafeNow != unsafeUtcNow)
			{
				// in case current machine runs NOT at UTC time
				Assert.AreNotEqual(unsafeNow, utcName.UserTime, "DateTime-utc");
			}
			Assert.AreEqual(unsafeUtcNow, utcName.UserTime, "DateTime-utc-utc");
			if (unsafeNow != unsafeLocalNow)
			{
				// in case current machine runs NOT at UTC-2 time
				Assert.AreNotEqual(unsafeLocalNow, utcName.UserTime, "DateTime-utc-local");
			}
		}

		[Test]
		public void Properties_Should_Work()
		{
			var pattern = new PatternF {AccountHRID = 17, ChunkID = Guid.NewGuid()};
			Assert.AreEqual(pattern.AccountHRID + "/" + pattern.ChunkID.ToString("N"), pattern.ToString());
		}
	}
}
