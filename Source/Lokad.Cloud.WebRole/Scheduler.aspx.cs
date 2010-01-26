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
			if (!IsPostBack)
			{
				ScheduleView.DataBind();
				ScheduleList.DataBind();
				ServiceList.DataBind();
			}
		}

		protected void ScheduleView_DataBinding(object sender, EventArgs e)
		{
			ScheduleView.DataSource = _cloudServiceScheduling.GetSchedules()
				.Select(info => new
					{
						Name = info.ServiceName,
						LastStarted = info.LastExecuted.PrettyFormatRelativeToNow(),
						Period = info.TriggerInterval,
						Scope = info.WorkerScoped ? "Worker" : "Cloud",
						Lease = PrettyFormatLease(info)
					})
				.ToList();
		}

		protected void ScheduleList_DataBinding(object sender, EventArgs e)
		{
			ScheduleList.DataSource = _cloudServiceScheduling.GetSchedules()
				.Select(info => info.ServiceName)
				.ToList();
		}

		protected void ServiceList_DataBinding(object sender, EventArgs e)
		{
			ServiceList.DataSource = _cloudServiceScheduling.GetScheduledUserServiceNames()
				.ToList();
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

		string PrettyFormatLease(ServiceSchedulingInfo info)
		{
			if (!info.LeasedSince.HasValue || !info.LeasedUntil.HasValue)
			{
				return "available";
			}

			var now = DateTimeOffset.Now;

			if (info.LeasedUntil.Value < now)
			{
				return "expired";
			}

			if (!info.LeasedBy.HasValue || String.IsNullOrEmpty(info.LeasedBy.Value))
			{
				return String.Format(
					"{0} ago, expires in {1}",
					now.Subtract(info.LeasedSince.Value).PrettyFormat(),
					info.LeasedUntil.Value.Subtract(now).PrettyFormat());
			}

			return String.Format(
				"by {0} {1} ago, expires in {2}",
				info.LeasedBy.Value,
				now.Subtract(info.LeasedSince.Value).PrettyFormat(),
				info.LeasedUntil.Value.Subtract(now).PrettyFormat());
		}
	}
}
