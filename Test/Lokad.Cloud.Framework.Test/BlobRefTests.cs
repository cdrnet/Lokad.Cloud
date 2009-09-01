#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using NUnit.Framework;

namespace Lokad.Cloud.Framework.Test
{
	[TestFixture]
	public class BlobRefTests
	{
		// ReSharper disable InconsistentNaming

		sealed class PatternA
		{
			public readonly DateTime Timestamp;
			public readonly long AccountHRID;
			public readonly Guid ChunkID;
			public readonly int ChunkSize;

			public PatternA(DateTime timestamp, long accountHrid, Guid chunkID, int chunkSize)
			{
				Timestamp = timestamp;
				AccountHRID = accountHrid;
				ChunkID = chunkID;
				ChunkSize = chunkSize;
			}
		}

		sealed class PatternB
		{
			public readonly long AccountHRID;
			public readonly Guid ChunkID;

			public PatternB(Guid chunkID, long accountHrid)
			{
				ChunkID = chunkID;
				AccountHRID = accountHrid;
			}
		}

		[Test]
		public void Conversion_round_trip()
		{
			var date = new DateTime(2009, 1, 1, 3, 4, 5);
			var original = new PatternA(date, 12000, Guid.NewGuid(), 120);

			var name = BlobRef.Print(original);

			Console.WriteLine(name);

			var parsed = BlobRef.Parse<PatternA>(name);
			Assert.AreNotSame(original, parsed);
			Assert.AreEqual(original.Timestamp, parsed.Timestamp);
			Assert.AreEqual(original.AccountHRID, parsed.AccountHRID);
			Assert.AreEqual(original.ChunkID, parsed.ChunkID);
			Assert.AreEqual(original.ChunkSize, parsed.ChunkSize);
		}

		[Test, ExpectedException(typeof(ArgumentException))]
		public void Wrong_type_is_detected()
		{
			var original = new PatternB(Guid.NewGuid(), 1000);
			var name = BlobRef.Print(original);
			BlobRef.Parse<PatternA>(name);
		}
	}
}
