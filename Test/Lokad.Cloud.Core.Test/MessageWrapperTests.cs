#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;

namespace Lokad.Cloud.Core.Test
{
	[TestFixture]
	public class MessageWrapperTests
	{
		[Test]
		public void Serialization()
		{
			// overflowing message
			var om = new MessageWrapper {IsOverflow = true, ContainerName = "con", BlobName = "blo"};

			var stream = new MemoryStream();
			var formatter = new BinaryFormatter();

			formatter.Serialize(stream, om);
			stream.Position = 0;
			var omBis = (MessageWrapper) formatter.Deserialize(stream);

			Assert.AreEqual(om.IsOverflow, omBis.IsOverflow, "#A00");
			Assert.AreEqual(om.ContainerName, omBis.ContainerName, "#A01");
			Assert.AreEqual(om.BlobName, omBis.BlobName, "#A02");
			
			// regular message
			om = new MessageWrapper { InnerMessage = "foobar"};

			stream.Position = 0;
			formatter.Serialize(stream, om);
			stream.Position = 0;
			omBis = (MessageWrapper)formatter.Deserialize(stream);

			Assert.AreEqual(om.IsOverflow, omBis.IsOverflow, "#A00");
			Assert.AreEqual(om.InnerMessage, omBis.InnerMessage, "#A01");
		}
	}
}
