#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Quality;
using NUnit.Framework;

namespace Lokad.Cloud.Test
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

			Assert.AreNotEqual(firstBlobName.Suffix, secondBlobName.Suffix, "two different temporaryBlobNames should have different suffix");
		}

		[Test]
		public void SpecializedTemporaryBlobNamesCanBeParsedAsBaseClass()
		{
			var now = DateTimeOffset.Now;
			// round to milliseconds, our DateTime resolution in blob names
			now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, now.Offset);

			var testName = new TestTemporaryBlobName(now, "test", Guid.NewGuid());
			var printed = BaseBlobName.Print(testName);

			var parsed = BaseBlobName.Parse<TemporaryBlobName>(printed);
			Assert.AreEqual(now, parsed.Expiration);
			Assert.AreEqual("test", parsed.Suffix);
		}

		private class TestTemporaryBlobName : BaseTemporaryBlobName<int>
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
