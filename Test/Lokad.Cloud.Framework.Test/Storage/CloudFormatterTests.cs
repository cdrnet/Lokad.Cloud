#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage.Test
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
				formatter.Serialize(test, stream);
				stream.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(test, formatter.Deserialize(stream, typeof(string)));
				//Console.WriteLine(stream.CanSeek);
			}

			using(var stream = new MemoryStream())
			{
				var test = 123;
				formatter.Serialize(test, stream);
				stream.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(test, formatter.Deserialize(stream, typeof(int)));
			}

			using(var stream = new MemoryStream())
			{
				var test = new byte[] { 1, 2, 3 };
				formatter.Serialize(test, stream);
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

				formatter.Serialize(items, stream);
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

				formatter.Serialize(item, stream);
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

				formatter.Serialize(hugeArray, stream);

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
				formatter.Serialize(item, stream);
				stream.Seek(0, SeekOrigin.Begin);
				var output = (MockWithObject)formatter.Deserialize(stream, typeof(MockWithObject));
				Assert.AreEqual(item.Generic, output.Generic);
			}
		}

		[Test]
		public void UnpackRepack()
		{
			var formatter = new CloudFormatter();

			// STRING
			foreach(var result in ApplyUnpackRepackXml(formatter, "test!"))
			{
				Assert.AreEqual("test!", result, "string");
			}

			// INTEGER
			foreach (var result in ApplyUnpackRepackXml(formatter, 123))
			{
				Assert.AreEqual(123, result, "integer");
			}

			// BYTE ARRAY
			var byteArray = new byte[] { 1, 2, 3 };
			foreach (var result in ApplyUnpackRepackXml(formatter, byteArray))
			{
				CollectionAssert.AreEquivalent(byteArray, result, "byte array");
			}
			
			// COMPLEX DATA
			var complexInput = new[]
				{
					new MockComplex
						{
							Prop1 = "0",
							Prop2 = 0,
							Flags = new List<MockEnum> {MockEnum.Item1},
							Ignored = "hi!",
							InvoiceId = "700",
							Field = 0.0F
						},
					new MockComplex
						{
							Prop1 = "1",
							Prop2 = 1,
							Flags = new List<MockEnum> {MockEnum.Item2},
							Ignored = "hi!",
							InvoiceId = "800",
							Field = 1.0F
						},
					new MockComplex
						{
							Prop1 = "2",
							Prop2 = 2,
							Flags = new List<MockEnum> {MockEnum.Item3, MockEnum.Item1},
							Ignored = "hi!",
							Field = 2.0F
						}
				};
			foreach (var result in ApplyUnpackRepackXml(formatter, complexInput))
			{
				Assert.AreEqual(complexInput.Length, result.Length);
				for (int i = 0; i < complexInput.Length; i++)
				{
					Assert.AreEqual(complexInput[i].Field, result[i].Field);
					Assert.AreEqual(complexInput[i].Prop1, result[i].Prop1);
					Assert.AreEqual(complexInput[i].Prop2, result[i].Prop2);
					CollectionAssert.AreEquivalent(complexInput[i].Flags, result[i].Flags);
					Assert.IsNull(result[i].Ignored);
					Assert.AreEqual(complexInput[i].InvoiceId, result[i].InvoiceId);
				}
			}

			// SERIALIZABLE INSTEAD OF DATA CONTRACT
			var serializableInput = new MockComplex2
			{
				Prop1 = "0",
				Prop2 = 0,
				Flags = new List<MockEnum> { MockEnum.Item1 },
			};
			foreach (var result in ApplyUnpackRepackXml(formatter, serializableInput))
			{
				Assert.AreEqual(serializableInput.Prop1, result.Prop1);
				Assert.AreEqual(serializableInput.Prop2, result.Prop2);
				Assert.AreEqual(serializableInput.Flags.Count, result.Flags.Count);
			}

			// UNTYPED DATA
			var unknownInput = new MockWithObject { Generic = DateTime.UtcNow.Second > 30 ? (object)100 : "hello" };
			foreach (var result in ApplyUnpackRepackXml(formatter, unknownInput))
			{
				Assert.AreEqual(unknownInput.Generic, result.Generic);
			}
		}

		IEnumerable<T> ApplyUnpackRepackXml<T>(IIntermediateDataSerializer formatter, T input)
		{
			XElement intermediate;
			using (var stream = new MemoryStream())
			{
				formatter.Serialize(input, stream);
				stream.Seek(0, SeekOrigin.Begin);
				intermediate = formatter.UnpackXml(stream);
			}

			// Direct
			using (var stream = new MemoryStream())
			{
				formatter.RepackXml(intermediate, stream);
				stream.Seek(0, SeekOrigin.Begin);
				yield return (T)formatter.Deserialize(stream, typeof(T));
			}

			// ToString/Parse Roundtrip
			{
				var text = intermediate.ToString();
				var xml = XElement.Parse(text);

				using (var stream = new MemoryStream())
				{
					formatter.RepackXml(xml, stream);
					stream.Seek(0, SeekOrigin.Begin);
					yield return (T)formatter.Deserialize(stream, typeof(T));
				}
			}

			// Save/Load Roundtrip
			{
				string text;
				using (var writer = new StringWriter())
				{
					intermediate.Save(writer);
					text = intermediate.ToString();
				}

				XElement xml;
				using (var reader = new StringReader(text))
				{
					xml = XElement.Load(reader);
				}

				using (var stream = new MemoryStream())
				{
					formatter.RepackXml(xml, stream);
					stream.Seek(0, SeekOrigin.Begin);
					yield return (T)formatter.Deserialize(stream, typeof(T));
				}
			}
		}

	}

}
