#region Copyright (c) Lokad 2009-2010
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
				LeaseList.DataBind();
			}
		}

		protected void ScheduleView_DataBinding(object sender, EventArgs e)
		{
			ScheduleView.DataSource = _cloudServiceScheduling.GetSchedules()
				.Select(info => new
					{
						Name = info.ServiceName,
						LastStarted = PrettyFormatLastExecuted(info),
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


		protected void LeaseList_DataBinding(object sender, EventArgs e)
		{
			LeaseList.DataSource = _cloudServiceScheduling.GetSchedules()
				.Where(info => info.LeasedSince.HasValue)
				.Select(info => info.ServiceName)
				.ToList();
		}

		protected void UpdateIntervalButton_OnClick(object sender, EventArgs e)
		{
			_cloudServiceScheduling.SetTriggerInterval(
				ScheduleList.SelectedValue,
				int.Parse(NewIntervalBox.Text).Seconds());

			ScheduleView.DataBind();
			ServiceList.DataBind();
			LeaseList.DataBind();
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
			ScheduleView.DataBind();
			LeaseList.DataBind();
		}

		protected void ReleaseButton_Click(object sender, EventArgs e)
		{
			Page.Validate("release");
			if (!Page.IsValid)
			{
				return;
			}

			var serviceName = LeaseList.SelectedValue;
			_cloudServiceScheduling.ReleaseLease(serviceName);

			LeaseList.DataBind();
			ScheduleView.DataBind();
			ServiceList.DataBind();
		}

		string PrettyFormatLastExecuted(ServiceSchedulingInfo info)
		{
			if(info.WorkerScoped)
			{
				return "untracked";
			}

			return info.LastExecuted.PrettyFormatRelativeToNow();
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
