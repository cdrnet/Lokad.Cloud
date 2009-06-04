#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Linq;
using NUnit.Framework;

// TODO: add tests for overflowing messages.

namespace Lokad.Cloud.Core.Test
{
	[TestFixture]
	public class QueueStorageProviderTests
	{
		private const string QueueName = "tests-queuestorageprovider-mymessage";

		[Test]
		public void PutGetDelete()
		{
			var provider = GlobalSetup.Container.Resolve<QueueStorageProvider>();

			Assert.IsNotNull(provider, "#A00");

			var message = new MyMessage();

			provider.Put(QueueName, new [] {message});
			var retrieved = provider.Get<MyMessage>(QueueName, 1).First();

			Assert.AreEqual(message.MyGuid, retrieved.MyGuid, "#A01");

			provider.Delete(QueueName, new [] { retrieved });
		}
	}

	[Serializable]
	public class MyMessage
	{
		public Guid MyGuid { get; private set; }

		public MyMessage()
		{
			MyGuid = Guid.NewGuid();
		}
	}
}
