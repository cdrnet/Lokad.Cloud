using System;
using NUnit.Framework;

namespace Lokad.Cloud.Test
{
	class MyBlobName : BaseTemporaryBlobName<double>
	{
		public readonly string MyDefaulPrefix;

		public MyBlobName(DateTime expiration, string prefix)
			: base(expiration, prefix)
		{
			MyDefaulPrefix = DefaultPrefix;
		}
	}

	[TestFixture]
	public class BaseTemporaryBlobNameTests
	{
		[Test]
		public void DefaultPrefix()
		{
			var name = new MyBlobName(DateTime.UtcNow, string.Empty);
			Assert.AreEqual("Lokad.Cloud.Test.MyBlobName", name.MyDefaulPrefix, "#A00");
		}
	}
}
