#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using Lokad.Cloud.Management;
using Lokad.Cloud.Diagnostics;
using System.Web.UI.WebControls;

namespace Lokad.Cloud.Web
{
	public partial class Monitoring : System.Web.UI.Page
	{
		readonly TimeSpan _cacheRefreshPeriod = 5.Minutes();
		readonly CloudStatistics _cloudStatistics = GlobalSetup.Container.Resolve<CloudStatistics>();

		protected void Page_Load(object sender, EventArgs e)
		{
			PartitionView.DataBind();
			ServiceView.DataBind();
			ProfilesView.DataBind();
			ExceptionsView.DataBind();
		}

		object ToPresentationModel(IEnumerable<PartitionStatistics> partitionStatistics)
		{
			return partitionStatistics
				.Select<PartitionStatistics, object>(s => new
					{
						Partition = s.PartitionKey,
						Runtime = s.Runtime,
						Cores = s.ProcessorCount,
						Threads = s.ThreadCount,
						Processing = PrettyFormatTimeSpan(s.TotalProcessorTime),
						Memory = PrettyFormatMemory(s.MemoryPrivateSize),
					})
				.ToList();
		}

		object ToPresentationModel(IEnumerable<ServiceStatistics> serviceStatistics)
		{
			return serviceStatistics
				.Select<ServiceStatistics, object>(s => new
					{
						Service = s.Name,
						Processing = PrettyFormatTimeSpan(s.TotalProcessorTime),
					})
				.ToList();
		}

		object ToPresentationModel(IEnumerable<ExecutionProfilingStatistics> profilingStatisticses)
		{
			return profilingStatisticses
				.SelectMany(s => s.Statistics
					.Where(d => d.OpenCount > 0)
					.Select(d => new
						{
							Context = s.Name,
							Name = d.Name,
							Count = d.OpenCount,
							Processing = PrettyFormatTimeSpan(TimeSpan.FromTicks(d.RunningTime)),
							Success = String.Format("{0}%", 100*d.CloseCount/d.OpenCount)
						}))
				.Take(25)
				.ToList();
		}

		object ToPresentationModel(IEnumerable<ExceptionTrackingStatistics> exceptionStatisticses)
		{
			return exceptionStatisticses
				.SelectMany(s => s.Statistics
					.Select(d => new
						{
							Context = s.Name,
							Count = d.Count,
							Text = d.Text
						}))
				.Take(25)
				.ToList();
		}

		static string PrettyFormatTimeSpan(TimeSpan timeSpan)
		{
			double delta = timeSpan.TotalSeconds;

			const int second = 1;
			const int minute = 60*second;
			const int hour = 60*minute;
			const int day = 24*hour;
			const int month = 30*day;

			if (delta < 1*minute) return timeSpan.Seconds == 1 ? "one second" : timeSpan.Seconds + " seconds";
			if (delta < 2*minute) return "a minute";
			if (delta < 45*minute) return timeSpan.Minutes + " minutes";
			if (delta < 90*minute) return "an hour";
			if (delta < 24*hour) return timeSpan.Hours + " hours";
			if (delta < 48*hour) return (int) timeSpan.TotalHours + " hours";
			if (delta < 30*day) return timeSpan.Days + " days";

			if (delta < 12*month)
			{
				var months = (int) Math.Floor(timeSpan.Days/30.0);
				return months <= 1 ? "one month" : months + " months";
			}

			var years = (int) Math.Floor(timeSpan.Days/365.0);
			return years <= 1 ? "one year" : years + " years";
		}

		private string PrettyFormatRelativeDateTime(DateTimeOffset dateTime)
		{
			var now = DateTimeOffset.Now;

			if(dateTime.UtcTicks == 0)
			{
				return String.Empty;
			}

			if(dateTime >= now)
			{
				return "just now";
			}

			return PrettyFormatTimeSpan(now - dateTime) + " ago";
		}

		static string PrettyFormatMemory(long byteCount)
		{
			return String.Format("{0} MB", byteCount/(1024*1024));
		}

		protected void PartitionView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<PartitionStatistics>(
				PartitionView,
				PartitionSelector,
				_cloudStatistics.GetPartitionsInPeriod,
				ToPresentationModel,
				"lokad-cloud-diag-partitions");
		}

		protected void ServiceView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<ServiceStatistics>(
				ServiceView,
				ServiceSelector,
				_cloudStatistics.GetServicesInPeriod,
				ToPresentationModel,
				"lokad-cloud-diag-services");
		}

		protected void ProfilesView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<ExecutionProfilingStatistics>(
				ProfilesView,
				ProfilesSelector,
				_cloudStatistics.GetExecutionProfilesInPeriod,
				ToPresentationModel,
				"lokad-cloud-diag-profiles");
		}

		protected void ExceptionsView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<ExceptionTrackingStatistics>(
				ExceptionsView,
				ExceptionsSelector,
				_cloudStatistics.GetTrackedExceptionsInPeriod,
				ToPresentationModel,
				"lokad-cloud-diag-exceptions");
		}

		void ApplySelectedDataSource<T>(
			BaseDataBoundControl target,
			ListControl selector,
			Func<TimeSegmentPeriod, DateTimeOffset, IEnumerable<T>> provider,
			Func<IEnumerable<T>, object> projector,
			string cacheNamePrefix)
		{
			var now = DateTimeOffset.Now;
			switch (selector.SelectedValue)
			{
				case "Today":
					target.DataSource = Cached(
						() => projector(provider(TimeSegmentPeriod.Day, now)),
						cacheNamePrefix + "-today");
					break;
				case "Yesterday":
					target.DataSource = Cached(
						() => projector(provider(TimeSegmentPeriod.Day, now.AddDays(-1))),
						cacheNamePrefix + "-yesterday");
					break;
				case "This Month":
					target.DataSource = Cached(
						() => projector(provider(TimeSegmentPeriod.Month, now)),
						cacheNamePrefix + "-thismonth");
					break;
				case "Last Month":
					target.DataSource = Cached(
						() => projector(provider(TimeSegmentPeriod.Month, now.AddMonths(-1))),
						cacheNamePrefix + "-lastmonth");
					break;
			}
		}

		T Cached<T>(Func<T> f, string key)
			where T : class
		{
			T value = Cache[key] as T;
			if (value == null)
			{
				Cache.Add(
					key,
					value = f(),
					null,
					DateTime.Now + _cacheRefreshPeriod,
					Cache.NoSlidingExpiration,
					CacheItemPriority.Normal,
					null);
			}

			return value;
		}

		
	}
}
