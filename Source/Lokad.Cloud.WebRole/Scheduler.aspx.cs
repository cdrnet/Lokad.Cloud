#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud;

namespace Lokad.Cloud.Web
{
	public partial class Scheduler : System.Web.UI.Page
	{
		// shorthand
		const string Cn = ScheduledService.ScheduleStateContainer;
		const string Prefix = ScheduledService.ScheduleStatePrefix;

		readonly IBlobStorageProvider _provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			//var states = GetStates().ToArray();

			// TODO: loading twice the states (not efficient)

			ScheduleView.DataSource = GetStates().Select(p => new
				{
					Name = p.Item1,
					p.Item2.LastExecuted,
					p.Item2.TriggerInterval
				});
			ScheduleView.DataBind();

			ScheduleList.DataSource = GetStates().Select(p => p.Item1);
			ScheduleList.DataBind();
		}

		IEnumerable<Pair<string, ScheduledServiceState>> GetStates()
		{
			foreach (var blobName in _provider.List(Cn, Prefix))
			{
				yield return new Pair<string, ScheduledServiceState>(
					// discarding the prefix for display purposes
					blobName.Substring(Prefix.Length + 1),
					_provider.GetBlob<ScheduledServiceState>(Cn, blobName));
			}
		}

		protected void UpdateIntervalButton_OnClick(object sender, EventArgs e)
		{
			var bn = Prefix + "/" + ScheduleList.SelectedValue;
			var triggerInterval = int.Parse(NewIntervalBox.Text);

			_provider.UpdateIfNotModified<ScheduledServiceState>(Cn, bn,
				state =>
					{
						state.TriggerInterval = triggerInterval.Seconds();
						return state;
					});

			ScheduleView.DataBind();
		}
	}
}
