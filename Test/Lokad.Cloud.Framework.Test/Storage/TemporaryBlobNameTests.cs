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
		public void TemporaryBlobReferencesAreUnique()
		{
			var expiration = new DateTime(2100, 12, 31);
			var firstBlobRef = TemporaryBlobName<int>.GetNew(expiration);
			var secondBlobRef = TemporaryBlobName<int>.GetNew(expiration);

			Assert.AreNotEqual(firstBlobRef.Suffix, secondBlobRef.Suffix, "two different temporary blob references should have different prefix");
		}

		[Test]
		public void SpecializedTemporaryBlobNamesCanBeParsedAsBaseClass()
		{
			var now = DateTimeOffset.UtcNow;
			// round to milliseconds, our time resolution in blob names
			now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, now.Offset);

			var testRef = new TestTemporaryBlobName(now, "test", Guid.NewGuid());
			var printed = UntypedBlobName.Print(testRef);

			var parsedRef = UntypedBlobName.Parse<TemporaryBlobName<object>>(printed);
			Assert.AreEqual(now, parsedRef.Expiration);
			Assert.AreEqual("test", parsedRef.Suffix);
		}

		private class TestTemporaryBlobName : TemporaryBlobName<int>
		{
			[UsedImplicitly, Rank(0)] public readonly Guid Id;

			public TestTemporaryBlobName(DateTimeOffset expiration, string prefix, Guid id)
				: base(expiration, prefix)
			{
				Id = id;
			}
		}
	}
}
