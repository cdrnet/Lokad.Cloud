
using System.Collections.Specialized;
using NUnit.Framework;

namespace Lokad.Cloud.Test
{
	[TestFixture]
	public class TypeInformationTests
	{
		[Test]
		public void GetInformation()
		{
			var type1 = typeof(TestClassForTypeInfo);
			var type2 = typeof(TestTransient1ClassForTypeInfo);
			var type3 = typeof(TestTransient2ClassForTypeInfo);

			var info1 = TypeInformation.GetInformation(type1);
			var info2 = TypeInformation.GetInformation(type2);
			var info3 = TypeInformation.GetInformation(type3);

			Assert.IsFalse(info1.IsTransient);
			Assert.IsFalse(info1.ThrowOnDeserializationError.HasValue);

			Assert.IsTrue(info2.IsTransient);
			Assert.IsFalse(info2.ThrowOnDeserializationError.Value);

			Assert.IsTrue(info3.IsTransient);
			Assert.IsTrue(info3.ThrowOnDeserializationError.Value);
		}

		[Test]
		public void SaveInBlobMetadata_LoadFromBlobMetadata()
		{
			var collection = new NameValueCollection();

			collection["OtherVal"] = "test";

			var type1 = typeof(TestClassForTypeInfo);
			var type2 = typeof(TestTransient1ClassForTypeInfo);
			var type3 = typeof(TestTransient2ClassForTypeInfo);

			var info1 = TypeInformation.GetInformation(type1);
			var info2 = TypeInformation.GetInformation(type2);
			var info3 = TypeInformation.GetInformation(type3);

			info1.SaveInBlobMetadata(collection);
			var info1Out = TypeInformation.LoadFromBlobMetadata(collection);
			Assert.IsTrue(info1.Equals(info1Out));
			Assert.AreEqual("test", collection["OtherVal"]);

			info2.SaveInBlobMetadata(collection);
			var info2Out = TypeInformation.LoadFromBlobMetadata(collection);
			Assert.IsTrue(info2.Equals(info2Out));
			Assert.AreEqual("test", collection["OtherVal"]);

			info3.SaveInBlobMetadata(collection);
			var info3Out = TypeInformation.LoadFromBlobMetadata(collection);
			Assert.IsTrue(info3.Equals(info3Out));
			Assert.AreEqual("test", collection["OtherVal"]);
		}

		internal class TestClassForTypeInfo
		{
			public string Field { get; set; }
		}

		[Transient(false)]
		internal class TestTransient1ClassForTypeInfo
		{
			public string Field { get; set; }
		}

		[Transient(true)]
		internal class TestTransient2ClassForTypeInfo
		{
			public string Field { get; set; }
		}
	}
}
