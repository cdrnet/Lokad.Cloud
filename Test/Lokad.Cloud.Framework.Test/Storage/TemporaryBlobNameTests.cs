#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Quality;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test
{
	[TestFixture]
	public class TemporaryBlobNameTests
	{
		[Test]
		public void TemporaryBlobNamesAreUnique()
		{
			var expiration = new DateTime(2100, 12, 31);
			var firstBlobName = TemporaryBlobName.GetNew(expiration);
			var secondBlobName = TemporaryBlobName.GetNew(expiration);

			Assert.AreNotEqual(firstBlobName.Suffix, secondBlobName.Suffix, "two different temporary blob names should have different prefix");
		}

		[Test]
		public void TemporaryBlobReferencesAreUnique()
		{
			var expiration = new DateTime(2100, 12, 31);
			var firstBlobRef = TemporaryBlobReference<int>.GetNew(expiration);
			var secondBlobRef = TemporaryBlobReference<int>.GetNew(expiration);

			Assert.AreNotEqual(firstBlobRef.Prefix, secondBlobRef.Prefix, "two different temporary blob references should have different prefix");
		}

		[Test]
		public void SpecializedTemporaryBlobNamesCanBeParsedAsBaseClass()
		{
			var now = DateTimeOffset.UtcNow;
			// round to milliseconds, our time resolution in blob names
			now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, now.Offset);

			var testRef = new TestTemporaryBlobReference(now, "test", Guid.NewGuid());
			var printed = BlobName.Print(testRef);

			var parsedRef = BlobName.Parse<TemporaryBlobName>(printed);
			Assert.AreEqual(now, parsedRef.Expiration);
			Assert.AreEqual("test", parsedRef.Suffix);
		}

		private class TestTemporaryBlobReference : TemporaryBlobReference<int>
		{
			[UsedImplicitly, Rank(0)] public readonly Guid Id;

			public TestTemporaryBlobReference(DateTimeOffset expiration, string prefix, Guid id)
				: base(expiration, prefix)
			{
				Id = id;
			}
		}
	}
}
