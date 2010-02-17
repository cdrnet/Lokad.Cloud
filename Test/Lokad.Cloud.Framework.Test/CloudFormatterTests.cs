#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class CloudFormatterTests
	{
		[DataContract]
		class MockWithObject
		{
			[DataMember]
			public object Generic { get; set; }
		}

		[DataContract]
		class MockComplex
		{
			[DataMember]
			public float Field;

			[DataMember]
			public string Prop1 { get; set; }

			[DataMember]
			public int Prop2 { get; set; }

			[DataMember]
			public List<MockEnum> Flags { get; set; }

			[DataMember(IsRequired = false)]
			public object InvoiceId { get; set; }

			public object Ignored { get; set; }
		}

		[Serializable]
		class MockComplex2
		{
			public string Prop1 { get; set; }
			public int Prop2 { get; set; }
			public List<MockEnum> Flags { get; set; }
		}

		[Serializable]
		enum MockEnum
		{
			Item1,
			Item2,
			Item3
		}

		[Test]
		public void SerializeDeserialize()
		{
			var formatter = new CloudFormatter();

			using(var stream = new MemoryStream())
			{
				var test = "hello!";
				formatter.Serialize(stream, test);
				stream.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(test, formatter.Deserialize(stream, typeof(string)));
				Console.WriteLine(stream.CanSeek);
			}

			using(var stream = new MemoryStream())
			{
				var test = 123;
				formatter.Serialize(stream, test);
				stream.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(test, formatter.Deserialize(stream, typeof(int)));
			}

			using(var stream = new MemoryStream())
			{
				var test = new byte[] { 1, 2, 3 };
				formatter.Serialize(stream, test);
				stream.Seek(0, SeekOrigin.Begin);
				CollectionAssert.AreEquivalent(test, (byte[])formatter.Deserialize(stream, typeof(byte[])));
			}

			using(var stream = new MemoryStream())
			{
				var items = new MockComplex[3];

				items[0] = new MockComplex
					{
					Prop1 = "0",
					Prop2 = 0,
					Flags = new List<MockEnum> { MockEnum.Item1 },
					Ignored = "hi!",
					InvoiceId = "700",
					Field = 0.0F
				};

				items[1] = new MockComplex
					{
					Prop1 = "1",
					Prop2 = 1,
					Flags = new List<MockEnum> { MockEnum.Item2 },
					Ignored = "hi!",
					InvoiceId = "800",
					Field = 1.0F
				};

				items[2] = new MockComplex
					{
					Prop1 = "2",
					Prop2 = 2,
					Flags = new List<MockEnum> { MockEnum.Item3, MockEnum.Item1 },
					Ignored = "hi!",
					Field = 2.0F
				};

				formatter.Serialize(stream, items);
				stream.Seek(0, SeekOrigin.Begin);

				var output = (MockComplex[])formatter.Deserialize(stream, typeof(MockComplex[]));

				Assert.AreEqual(items.Length, output.Length);
				for(int i = 0; i < items.Length; i++)
				{
					Assert.AreEqual(items[i].Field, output[i].Field);
					Assert.AreEqual(items[i].Prop1, output[i].Prop1);
					Assert.AreEqual(items[i].Prop2, output[i].Prop2);
					CollectionAssert.AreEquivalent(items[i].Flags, output[i].Flags);
					Assert.IsNull(output[i].Ignored);
					Assert.AreEqual(items[i].InvoiceId, output[i].InvoiceId);
				}
			}

			using (var stream = new MemoryStream())
			{
				var item = new MockComplex2
					{
						Prop1 = "0",
						Prop2 = 0,
						Flags = new List<MockEnum> {MockEnum.Item1},
					};

				formatter.Serialize(stream, item);
				stream.Seek(0, SeekOrigin.Begin);

				var output = (MockComplex2)formatter.Deserialize(stream, typeof(MockComplex2));

				Assert.AreEqual(item.Prop1, output.Prop1);
				Assert.AreEqual(item.Prop2, output.Prop2);
				Assert.AreEqual(item.Flags.Count, output.Flags.Count);
			}

			// Test "big" object
			using(var stream = new MemoryStream())
			{
				var hugeArray = new double[10000];
				for(int i = 0; i < hugeArray.Length; i++)
				{
					hugeArray[i] = i;
				}

				formatter.Serialize(stream, hugeArray);

				stream.Seek(0, SeekOrigin.Begin);
				var output = (double[])formatter.Deserialize(stream, typeof(double[]));

				CollectionAssert.AreEquivalent(hugeArray, output);
			}
		}

		[Test]
		public void UnknownType()
		{
			var formatter = new CloudFormatter();

			using(var stream = new MemoryStream())
			{
				var item = new MockWithObject { Generic = DateTime.UtcNow.Second > 30 ? (object)100 : "hello" };
				formatter.Serialize(stream, item);
				stream.Seek(0, SeekOrigin.Begin);
				var output = (MockWithObject)formatter.Deserialize(stream, typeof(MockWithObject));
				Assert.AreEqual(item.Generic, output.Generic);
			}
		}

	}

}
