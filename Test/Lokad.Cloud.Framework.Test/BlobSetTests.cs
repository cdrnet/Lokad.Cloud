#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;

using BlobSet = Lokad.Cloud.BlobSet<object>;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class BlobSetTests
	{
		// validating the anonymous methods could be serialized
		[Test]
		public void FuncSerialization()
		{
			var myFunc = new Func<int, int>(i => i*i);

			var stream = new MemoryStream();
			var formatter = new BinaryFormatter();

			formatter.Serialize(stream, myFunc);

			stream.Position = 0;
			var myFuncBis = (Func<int, int>) formatter.Deserialize(stream);

			Assert.AreEqual(myFunc(5), myFuncBis(5), "#A00");
		}

		// validating the invocation through reflection of functions.
		[Test]
		public void InvokeDelegate()
		{
			var myFunc = new Func<int, int>(i => i * i);

			Assert.AreEqual(myFunc(5), BlobSet.InvokeAsDelegate(myFunc, 5), "#A00");
		}
	}
}
