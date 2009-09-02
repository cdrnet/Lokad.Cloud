#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Web
{
	public partial class Workloads : System.Web.UI.Page
	{
		readonly IQueueStorageProvider _provider = GlobalSetup.Container.Resolve<IQueueStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			QueuesView.DataSource = GetQueues();
			QueuesView.DataBind();
		}

		IEnumerable<object> GetQueues()
		{
			foreach(var queueName in _provider.List(null))
			{
				var inQueueCount = _provider.GetApproximateCount(queueName);

				yield return new
					{
						QueueName = queueName,
						Count = inQueueCount
					};
			}
		}
	}
}
