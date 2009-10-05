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
	/// <summary>Processes orders.</summary>
	[QueueServiceSettings(AutoStart = true, QueueName = OrderDispatcher.QueueName)]
	public class OrderProcessorService : QueueService<OrderMessage>
	{
		protected override void Start(OrderMessage message)
		{
			// Process order...
			// message is automatically removed from the queue by Lokad.Cloud upon completion of this method
			// If this method throws, the message automatically reappears in the queue
		}
	}
}
