#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud;

namespace TestingSample
{
	/// <summary>Dispatches orders.</summary>
	public class OrderDispatcher
	{
		/// <summary>The name of the queue.</summary>
		public const string QueueName = "orders";

		IQueueStorageProvider _queueStorage;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:OrderDispatcher"/> class.
		/// </summary>
		/// <param name="queueStorage">The queue storage provider.</param>
		public OrderDispatcher(IQueueStorageProvider queueStorage)
		{
			_queueStorage = queueStorage;
		}

		/// <summary>Places an order.</summary>
		/// <param name="supplierId">The ID of the supplier.</param>
		/// <param name="parts">The IDs of the parts to order.</param>
		public void PlaceOrder(string supplierId, IEnumerable<string> parts)
		{
			_queueStorage.Put(QueueName,
				new OrderMessage() { OrderPlacedOn = DateTime.Now, SupplierId = supplierId, Parts = new List<string>(parts) });
		}
	}
}
