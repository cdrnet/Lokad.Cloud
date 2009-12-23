#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using System.Web.UI.WebControls;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;

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
						//OS = PrettyFormatOperatingSystem(s.OperatingSystem),
						Runtime = s.Runtime,
						Cores = s.ProcessorCount,
						Threads = s.ThreadCount,
						Total = s.TotalProcessorTime.PrettyFormat(),
						Kernel = (s.TotalProcessorTime - s.UserProcessorTime).PrettyFormat(),
						Active = PrettyFormatActiveTime(s.ActiveTime, s.StartCount),
						Memory = PrettyFormatMemoryMB(s.MemoryPrivateSize),
					})
				.Take(50)
				.ToList();
		}

		object ToPresentationModel(IEnumerable<ServiceStatistics> serviceStatistics)
		{
			return serviceStatistics
				.Select<ServiceStatistics, object>(s => new
					{
						Service = s.Name,
						Total = s.TotalProcessorTime.PrettyFormat(),
						Kernel = (s.TotalProcessorTime - s.UserProcessorTime).PrettyFormat()
					})
				.Take(50)
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
							Total = TimeSpan.FromTicks(d.RunningTime).PrettyFormat(),
							Average = d.CloseCount == 0 ? "N/A" : TimeSpan.FromTicks(d.RunningTime / d.CloseCount).PrettyFormat(),
							Success = String.Format("{0}%", 100 * d.CloseCount / d.OpenCount)
						}))
				.Take(50)
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
							Message = d.Message,
							Text = d.Text
						}))
				.Take(25)
				.ToList();
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

		public static string PrettyFormatMemoryMB(long byteCount)
		{
			return String.Format("{0} MB", byteCount / (1024 * 1024));
		}

		public static string PrettyFormatOperatingSystem(string os)
		{
			if (string.IsNullOrEmpty(os))
			{
				return string.Empty;
			}

			os = os.Replace("Microsoft Windows ", string.Empty);
			return os.Replace("Service Pack ", "SP");
		}

		public static string PrettyFormatActiveTime(TimeSpan activeTime, int startCount)
		{
			var time = activeTime.PrettyFormat();
			if (startCount == 1)
			{
				return String.Concat(time, " (one restart)");
			}
			if (startCount > 0)
			{
				return String.Concat(time, " (", startCount.ToString(), " restarts)");
			}
			return time;
		}
	}
}
