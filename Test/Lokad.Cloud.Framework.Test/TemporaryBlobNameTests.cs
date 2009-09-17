#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	}
}
