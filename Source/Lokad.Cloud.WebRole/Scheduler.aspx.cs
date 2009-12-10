#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Web
{
	public partial class Scheduler : System.Web.UI.Page
	{
		readonly CloudServiceScheduling _cloudServiceScheduling = GlobalSetup.Container.Resolve<CloudServiceScheduling>();

		protected void Page_Load(object sender, EventArgs e)
		{
			ScheduleView.DataBind();
			ScheduleList.DataBind();
			ServiceList.DataBind();
		}

		protected void ScheduleView_DataBinding(object sender, EventArgs e)
		{
			ScheduleView.DataSource = _cloudServiceScheduling.GetSchedules()
				.Select(info => new
					{
						Name = info.ServiceName,
						info.LastExecuted,
						info.TriggerInterval
					});
		}

		protected void ScheduleList_DataBinding(object sender, EventArgs e)
		{
			ScheduleList.DataSource = _cloudServiceScheduling.GetSchedules()
				.Select(info => info.ServiceName);
		}

		protected void ServiceList_DataBinding(object sender, EventArgs e)
		{
			ServiceList.DataSource = _cloudServiceScheduling.GetScheduledUserServiceNames();
		}

		protected void UpdateIntervalButton_OnClick(object sender, EventArgs e)
		{
			_cloudServiceScheduling.SetTriggerInterval(
				ScheduleList.SelectedValue,
				int.Parse(NewIntervalBox.Text).Seconds());

			ScheduleView.DataBind();
		}

		protected void DeleteButton_Click(object sender, EventArgs e)
		{
			Page.Validate("delete");
			if (!Page.IsValid)
			{
				return;
			}

			var serviceName = ServiceList.SelectedValue;
			_cloudServiceScheduling.RemoveSchedule(serviceName);

			ServiceList.DataBind();
		}
	}
}
