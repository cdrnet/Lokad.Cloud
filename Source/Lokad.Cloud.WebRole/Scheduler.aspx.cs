#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cloud.Web
{
	public partial class Scheduler : System.Web.UI.Page
	{
		readonly IBlobStorageProvider _provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			//var states = GetStates().ToArray();

			// TODO: loading twice the states (not efficient)

			ScheduleView.DataSource = GetStates().Where(s => null != s).Select(p => new
				{
					Name = p.Item1,
					p.Item2.LastExecuted,
					p.Item2.TriggerInterval
				});
			ScheduleView.DataBind();

			ScheduleList.DataSource = GetStates().Where(s => null != s).Select(p => p.Item1);
			ScheduleList.DataBind();
		}

		IEnumerable<Pair<string, ScheduledServiceState>> GetStates()
		{
			foreach (var blobName in _provider.List(ScheduledServiceStateName.GetPrefix()))
			{
				yield return new Pair<string, ScheduledServiceState>(
					// discarding the prefix for display purposes
					blobName.ServiceName,
					_provider.GetBlobOrDelete<ScheduledServiceState>(blobName));
			}
		}

		protected void UpdateIntervalButton_OnClick(object sender, EventArgs e)
		{
			var blobName = new ScheduledServiceStateName(ScheduleList.SelectedValue);
			var triggerInterval = int.Parse(NewIntervalBox.Text);

			_provider.UpdateIfNotModified<ScheduledServiceState>(blobName,
				state =>
					{
						state.TriggerInterval = triggerInterval.Seconds();
						return state;
					});

			ScheduleView.DataBind();
		}
	}
}
