using System;
using System.Linq;
using System.Web.Caching;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Web
{
	public partial class Monitoring : System.Web.UI.Page
	{
		readonly ServiceMonitor _services = (ServiceMonitor)GlobalSetup.Container.Resolve<IServiceMonitor>();
		readonly PartitionMonitor _partitions = new PartitionMonitor(GlobalSetup.Container.Resolve<IBlobStorageProvider>());
		readonly ExecutionProfilingMonitor _profiles = new ExecutionProfilingMonitor(GlobalSetup.Container.Resolve<IBlobStorageProvider>());
		readonly ExceptionTrackingMonitor _exceptions = new ExceptionTrackingMonitor(GlobalSetup.Container.Resolve<IBlobStorageProvider>());

		protected void Page_Load(object sender, EventArgs e)
		{
			PartitionView.DataSource = Cached(
				() => _partitions.GetStatistics()
					.Select(s => new
						{
							Partition = s.PartitionKey,
							Runtime = s.Runtime,
							Cores = s.ProcessorCount,
							Threads = s.ThreadCount,
							Processing =  PrettyFormatTimeSpan(s.TotalProcessorTime),
							Memory = PrettyFormatMemory(s.MemoryPrivateSize),
							//Updated = PrettyFormatRelativeDateTime(s.LastUpdate)
						})
					.ToList(),
				"lokad-cloud-diag-partitions");
			PartitionView.DataBind();

			ServiceView.DataSource = Cached(
				() => _services.GetStatistics()
					.Select(s => new
						{
							Service = s.Name,
							Processing = PrettyFormatTimeSpan(s.TotalProcessorTime),
							Since = PrettyFormatRelativeDateTime(s.FirstStartTime),
							//Updated = PrettyFormatRelativeDateTime(s.LastUpdate)
						})
					.ToList(),
				"lokad-cloud-diag-services");
			ServiceView.DataBind();

			ExecutionProfilesView.DataSource = Cached(
				() => _profiles.GetStatistics()
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
					.ToList(),
				"lokad-cloud-diag-profiles");
			ExecutionProfilesView.DataBind();

			TrackedExceptionsView.DataSource = Cached(
				() => _exceptions.GetStatistics()
					.SelectMany(s => s.Statistics
						.Select(d => new
							{
								Context = s.Name,
								Count = d.Count,
								Text = d.Text
							}))
					.Take(25)
					.ToList(),
				"lokad-cloud-diag-exceptions");
			TrackedExceptionsView.DataBind();
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
					DateTime.Now + TimeSpan.FromMinutes(5),
					Cache.NoSlidingExpiration,
					CacheItemPriority.Normal,
					null);
			}

			return value;
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
	}
}
