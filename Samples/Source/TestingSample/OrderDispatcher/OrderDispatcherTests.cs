#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Lokad.Cloud;

namespace TestingSample
{
	[TestFixture]
	public class OrderDispatcherTests
	{
		[Test]
		public void PlaceOrder()
		{
			var queueStorage = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

			var dispatcher = new OrderDispatcher(queueStorage);

			dispatcher.PlaceOrder("SUP01", new [] { "PRT01", "PRT09" });

			var order = queueStorage.Get<OrderMessage>(OrderDispatcher.QueueName, 1).FirstOrDefault();

			Assert.AreEqual("SUP01", order.SupplierId, "Wrong supplier ID");
			Assert.AreEqual(2, order.Parts.Count, "Wrong part count");
			Assert.AreEqual("PRT01", order.Parts[0], "Wrong part ID");
			Assert.AreEqual("PRT09", order.Parts[1], "Wrong part ID");
		}
	}
}
