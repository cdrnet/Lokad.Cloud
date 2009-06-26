#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Core;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Web
{
	public partial class Scheduler : System.Web.UI.Page
	{
		readonly IBlobStorageProvider _provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			ScheduleView.DataSource = GetSchedules();
			ScheduleView.DataBind();
		}

		IEnumerable<object> GetSchedules()
		{
			var cn = ScheduledService.ScheduleStateContainer;
			var prefix = ScheduledService.ScheduleStatePrefix;

			foreach (var blobName in _provider.List(cn, prefix))
			{
				var state = _provider.GetBlob<ScheduledServiceState>(cn, blobName);
				yield return new
				{
					Name = blobName.Substring(prefix.Length + 1), // discarding the prefix
					state.LastExecuted,
					state.TriggerInterval
				};
			}
		}
	}
}
