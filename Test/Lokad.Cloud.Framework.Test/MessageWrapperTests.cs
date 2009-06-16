#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;

namespace Lokad.Cloud.Framework.Test
{
	[TestFixture]
	public class MessageWrapperTests
	{
		[Test]
		public void Serialization()
		{
			// overflowing message
			var om = new MessageWrapper {ContainerName = "con", BlobName = "blo"};

			var stream = new MemoryStream();
			var formatter = new BinaryFormatter();

			formatter.Serialize(stream, om);
			stream.Position = 0;
			var omBis = (MessageWrapper) formatter.Deserialize(stream);

			Assert.AreEqual(om.ContainerName, omBis.ContainerName, "#A00");
			Assert.AreEqual(om.BlobName, omBis.BlobName, "#A01");
		}
	}
}
