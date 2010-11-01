﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using System.Web.UI.WebControls;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Web
{
	public partial class Monitoring : System.Web.UI.Page
	{
		readonly TimeSpan _cacheRefreshPeriod = 2.Minutes();
		readonly ICloudStatisticsApi _cloudStatistics = GlobalSetup.Container.Resolve<ICloudStatisticsApi>();

		protected void Page_Load(object sender, EventArgs e)
		{
			PartitionView.DataBind();
			ServiceView.DataBind();
			ProfilesView.DataBind();
		}

		static object ToPresentationModel(IEnumerable<PartitionStatistics> partitionStatistics)
		{
			return partitionStatistics
				.Select<PartitionStatistics, object>(s => new
					{
						Partition = s.PartitionKey,
						Runtime = s.Runtime,
						Cores = s.ProcessorCount,
						Threads = s.ThreadCount,
						CPU = s.TotalProcessorTime.PrettyFormat(),
						//Kernel = (s.TotalProcessorTime - s.UserProcessorTime).PrettyFormat(),
						Active = PrettyFormatActiveTime(s.ActiveTime, s.StartCount),
						Usage = PrettyFormatUsage(s.TotalProcessorTime, s.ActiveTime),
						Memory = PrettyFormatMemoryMB(s.MemoryPrivateSize),
					})
				.Take(50)
				.ToList();
		}

		static object ToPresentationModel(IEnumerable<ServiceStatistics> serviceStatistics)
		{
			return serviceStatistics
				.Select<ServiceStatistics, object>(s => new
					{
						Service = s.Name,
						Count = s.Count,
						CPU = s.TotalProcessorTime.PrettyFormat(),
						//Kernel = (s.TotalProcessorTime - s.UserProcessorTime).PrettyFormat()
						Total = s.AbsoluteTime.PrettyFormat(),
						//Average = s.Count == 0 ? "N/A" : TimeSpan.FromTicks(s.AbsoluteTime.Ticks / s.Count).PrettyFormat(),
						Max = s.MaxAbsoluteTime.PrettyFormat(),
						Usage = PrettyFormatUsage(s.TotalProcessorTime, s.AbsoluteTime),
					})
				.Take(50)
				.ToList();
		}

		static object ToPresentationModel(IEnumerable<ExecutionProfilingStatistics> profilingStatisticses)
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

		protected void PartitionView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<PartitionStatistics>(
				PartitionView,
				PartitionSelector,
				_cloudStatistics.GetPartitionsOfMonth,
				_cloudStatistics.GetPartitionsOfDay,
				ToPresentationModel,
				"lokad-cloud-diag-partitions");
		}

		protected void ServiceView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<ServiceStatistics>(
				ServiceView,
				ServiceSelector,
				_cloudStatistics.GetServicesOfMonth,
				_cloudStatistics.GetServicesOfDay,
				ToPresentationModel,
				"lokad-cloud-diag-services");
		}

		protected void ProfilesView_DataBinding(object sender, EventArgs e)
		{
			ApplySelectedDataSource<ExecutionProfilingStatistics>(
				ProfilesView,
				ProfilesSelector,
				_cloudStatistics.GetProfilesOfMonth,
				_cloudStatistics.GetProfilesOfDay,
				ToPresentationModel,
				"lokad-cloud-diag-profiles");
		}

		void ApplySelectedDataSource<T>(
			BaseDataBoundControl target,
			ListControl selector,
			Func<DateTime?, IEnumerable<T>> monthProvider,
			Func<DateTime?, IEnumerable<T>> dayProvider,
			Func<IEnumerable<T>, object> projector,
			string cacheNamePrefix)
		{
			var now = DateTimeOffset.UtcNow;
			switch (selector.SelectedValue)
			{
				case "Today":
					target.DataSource = Cached(
						() => projector(dayProvider(now.UtcDateTime)),
						cacheNamePrefix + "-today");
					break;
				case "Yesterday":
					target.DataSource = Cached(
						() => projector(dayProvider(now.AddDays(-1).UtcDateTime)),
						cacheNamePrefix + "-yesterday");
					break;
				case "This Month":
					target.DataSource = Cached(
						() => projector(monthProvider(now.UtcDateTime)),
						cacheNamePrefix + "-thismonth");
					break;
				case "Last Month":
					target.DataSource = Cached(
						() => projector(monthProvider(now.AddMonths(-1).UtcDateTime)),
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
					DateTime.UtcNow + _cacheRefreshPeriod,
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

		public static string PrettyFormatUsage(TimeSpan cpuTime, TimeSpan activeTime)
		{
			var activeSeconds = activeTime.TotalSeconds;
			if(activeSeconds == 0d)
			{
				return "0%";
			}

			return Math.Round(cpuTime.TotalSeconds/activeSeconds*100d) + "%";
		}
	}
}
