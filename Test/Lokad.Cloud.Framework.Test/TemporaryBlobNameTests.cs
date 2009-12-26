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
			var dateTime = DateTime.UtcNow;
			var testName = new TestTemporaryBlobName(dateTime, "test", Guid.NewGuid());
			var printed = BaseBlobName.Print(testName);

			var parsed = BaseBlobName.Parse<TemporaryBlobName>(printed);
			Assert.AreEqual(dateTime.ToString(), parsed.Expiration.ToString());
			Assert.AreEqual("test", parsed.Suffix);
		}

		private class TestTemporaryBlobName : BaseTemporaryBlobName<int>
		{
			[UsedImplicitly, Rank(0)] public readonly Guid Id;

			public TestTemporaryBlobName(DateTime expiration, string prefix, Guid id)
				: base(expiration, prefix)
			{
				Id = id;
			}
		}
	}
}
