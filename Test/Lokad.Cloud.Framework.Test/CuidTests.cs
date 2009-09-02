#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud.Framework;
using NUnit.Framework;
using NUnit.Framework.Extensions;
using System.Threading;

// TODO: [vermorel] 2009-07-23, excluded from the built
// Focusing on a minimal amount of feature for the v0.1
// will be reincluded later on.

namespace Lokad.Cloud.Framework.Test
{
	[TestFixture]
	public class CuidTests
	{
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Next_NullProvider()
		{
			Cuid.Next(null, "counter");
		}

		[RowTest]
		[Row(null, ExpectedException = typeof(ArgumentNullException))]
		[Row("", ExpectedException = typeof(ArgumentException))]
		public void Next_InvalidCounterId(string counterId)
		{
			Cuid.Next(new InMemoryBlobStorageProvider(), counterId);
		}

		[Test]
		public void Next()
		{
			// Given the simplicity (no concurrency) of this test and
			// knowledge of the inner workings of Cuid,
			// it's fine to assume that generated CUIDs are sequential

			InMemoryBlobStorageProvider provider = new InMemoryBlobStorageProvider();

			Assert.AreEqual(1, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(2, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(3, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(4, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(5, Cuid.Next(provider, "mycounter"));

			Assert.AreEqual(1, Cuid.Next(provider, "mycounter2"));
			Assert.AreEqual(2, Cuid.Next(provider, "mycounter2"));
			Assert.AreEqual(3, Cuid.Next(provider, "mycounter2"));
			Assert.AreEqual(4, Cuid.Next(provider, "mycounter2"));

			Assert.AreEqual(6, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(7, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(8, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(9, Cuid.Next(provider, "mycounter"));
			Assert.AreEqual(10, Cuid.Next(provider, "mycounter"));

			// Inner knowledge: blob name equals counter ID
			long nextId1 = provider.GetBlob<long>(Cuid.ContainerName, "mycounter");
			long nextId2 = provider.GetBlob<long>(Cuid.ContainerName, "mycounter2");
			Assert.IsTrue(nextId1 > 11, "Exponential eager allocation does not work");
			Assert.IsTrue(nextId2 > 5, "Exponential eager allocation does not work");
		}

	}

}
