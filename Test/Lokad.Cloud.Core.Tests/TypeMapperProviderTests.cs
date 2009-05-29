#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using NUnit.Framework;

namespace Lokad.Cloud.Core.Tests
{
	[TestFixture]
	public class TypeMapperProviderTests
	{
		[Test]
		public void GetStorageName()
		{
			var mapper = new TypeMapperProvider();
			Assert.AreEqual(
				"lokad-cloud-core-tests-typemapperprovidertests", 
				mapper.GetStorageName(typeof(TypeMapperProviderTests)));
		}
	}
}
