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

			ServiceList.DataBind();
		}

		IEnumerable<Pair<string, ScheduledServiceState>> GetStates()
		{
			foreach (var blobName in _provider.List(ScheduledServiceStateName.GetPrefix()))
			{
				var blob = _provider.GetBlobOrDelete(blobName);
				if (!blob.HasValue)
				{
					continue;
				}

				yield return new Pair<string, ScheduledServiceState>(
					blobName.ServiceName,
					blob.Value);
			}
		}

		protected void UpdateIntervalButton_OnClick(object sender, EventArgs e)
		{
			var blobName = new ScheduledServiceStateName(ScheduleList.SelectedValue);
			var triggerInterval = int.Parse(NewIntervalBox.Text);

			_provider.UpdateIfNotModified(blobName,
				s =>
					{
						var state = s.Value;
						state.TriggerInterval = triggerInterval.Seconds();
						return state;
					});

			ScheduleView.DataBind();
		}

		protected void ServiceList_DataBinding(object sender, EventArgs e)
		{
			// Filter out built-in services
			var services = new List<string>();

			foreach(var name in _provider.List(ScheduledServiceStateName.GetPrefix()))
			{
				// HACK: name of built-in services is hard-coded
				if(name.ServiceName != typeof(Cloud.Services.GarbageCollectorService).FullName &&
					name.ServiceName != typeof(Cloud.Services.DelayedQueueService).FullName &&
					name.ServiceName != typeof(Cloud.Services.MonitoringService).FullName)
				{
					services.Add(name.ServiceName);
				}
			}

			ServiceList.DataSource = services;
		}

		protected void DeleteButton_Click(object sender, EventArgs e)
		{
			Page.Validate("delete");
			if(!Page.IsValid) return;

			var serviceName = ServiceList.SelectedValue;

			var stateBlobName = new ScheduledServiceStateName(serviceName);

			_provider.DeleteBlob(stateBlobName);

			ServiceList.DataBind();
		}
	}
}
