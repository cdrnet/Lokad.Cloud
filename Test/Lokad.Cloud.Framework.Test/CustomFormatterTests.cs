#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class CustomFormatterTests
	{
		[Test]
		public void Serialize_Deserialize()
		{
			CustomFormatter formatter = new CustomFormatter();

			using(var stream = new MemoryStream())
			{
				var test = "hello!";
				formatter.Serialize(stream, test);
				long size = stream.Length;
				stream.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(test, formatter.Deserialize<string>(stream));
				Console.WriteLine(stream.CanSeek);
			}

			using(var stream = new MemoryStream())
			{
				var test = 123;
				formatter.Serialize(stream, test);
				stream.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(test, formatter.Deserialize<int>(stream));
			}

			using(var stream = new MemoryStream())
			{
				var test = new byte[] { 1, 2, 3 };
				formatter.Serialize(stream, test);
				stream.Seek(0, SeekOrigin.Begin);
				CollectionAssert.AreEquivalent(test, formatter.Deserialize<byte[]>(stream));
			}

			using(var stream = new MemoryStream())
			{
				var items = new TestClass[3];

				items[0] = new TestClass()
				{
					Prop1 = "0",
					Prop2 = 0,
					Flags = new List<TestEnum>() { TestEnum.Item1 },
					Ignored = "hi!",
					InvoiceId = "700",
					Field = 0.0F
				};

				items[1] = new TestClass()
				{
					Prop1 = "1",
					Prop2 = 1,
					Flags = new List<TestEnum>() { TestEnum.Item2 },
					Ignored = "hi!",
					InvoiceId = "800",
					Field = 1.0F
				};

				items[2] = new TestClass()
				{
					Prop1 = "2",
					Prop2 = 2,
					Flags = new List<TestEnum>() { TestEnum.Item3, TestEnum.Item1 },
					Ignored = "hi!",
					Field = 2.0F
				};

				formatter.Serialize(stream, items);
				stream.Seek(0, SeekOrigin.Begin);

				var output = formatter.Deserialize<TestClass[]>(stream);

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

			// Test required fields
			using(var stream = new MemoryStream())
			{
				var item = new TestClass()
				{
					Field = 0,
					Prop1 = null,
					Prop2 = 10,
					Flags = new List<TestEnum>()
				};

				formatter.Serialize(stream, item);

				Assert.Throws<SerializationException>(() => formatter.Deserialize<TestClass>(stream));
			}

			// Test "big" object
			using(var stream = new MemoryStream())
			{
				var hugeArray = new double[10000];
				for(int i = 0; i < hugeArray.Length; i++)
				{
					hugeArray[i] = i;
				}

				DateTime begin = DateTime.Now;
				formatter.Serialize(stream, hugeArray);
				var time = DateTime.Now - begin;
				long size = stream.Length;

				stream.Seek(0, SeekOrigin.Begin);
				begin = DateTime.Now;
				var output = formatter.Deserialize<double[]>(stream);
				time = DateTime.Now - begin;

				CollectionAssert.AreEquivalent(hugeArray, output);
			}
		}

		[Test]
		public void UnknownType()
		{
			CustomFormatter formatter = new CustomFormatter();

			using(var stream = new MemoryStream())
			{
				var item = new WithObject() { Generic = DateTime.Now.Second > 30 ? (object)100 : (object)"hello" };
				formatter.Serialize(stream, item);
				stream.Seek(0, SeekOrigin.Begin);
				var output = formatter.Deserialize<WithObject>(stream);
				Assert.AreEqual(item.Generic, output.Generic);
			}
		}
	}

	[DataContract]
	internal class WithObject
	{
		[DataMember]
		public object Generic { get; set; }
	}

	[DataContract]
	internal class Container
	{
		[DataMember]
		public TestClass Item { get; set; }
	}

	[DataContract]
	internal class TestClass
	{
		[DataMember]
		public float Field;

		[DataMember]
		public string Prop1 { get; set; }

		[DataMember]
		public int Prop2 { get; set; }

		[DataMember]
		public List<TestEnum> Flags { get; set; }

		[DataMember(IsRequired = false)]
		public object InvoiceId { get; set; }

		public object Ignored { get; set; }
	}

	[Serializable]
	internal enum TestEnum
	{
		Item1,
		Item2,
		Item3
	}
}
