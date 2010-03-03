#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using Lokad.Cloud.Mock;
using NUnit.Framework;


namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class CloudTableTests
	{
		[Test]
		public void TableNameValidation()
		{
			var mockProvider = new MemoryTableStorageProvider();

			new CloudTable<int>(mockProvider, "abc"); // name is OK

			try
			{
				new CloudTable<int>(mockProvider, "ab"); // name too short
				Assert.Fail("#A00");
			}
			catch(ArgumentException) {}

			try
			{
				new CloudTable<int>(mockProvider, "ab-sl"); // hyphen not permitted
				Assert.Fail("#A01");
			}
			catch (ArgumentException) { }
		}

	}
}
