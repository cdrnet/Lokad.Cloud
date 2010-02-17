#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

// TODO: Discard old data

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Service Monitoring Data Provider
	/// </summary>
	internal class ServiceMonitor : IServiceMonitor
	{
		static List<ServiceStatisticUpdate> _updates = new List<ServiceStatisticUpdate>();
		static readonly object _updatesSync = new object();

		readonly ICloudDiagnosticsRepository _repository;

		/// <summary>
		/// Creates an instance of the <see cref="ServiceMonitor"/> class.
		/// </summary>
		public ServiceMonitor(ICloudDiagnosticsRepository repository)
		{
			_repository = repository;
		}

		public void UpdateStatistics()
		{
			List<ServiceStatisticUpdate> updates;
			lock(_updatesSync)
			{
				updates = _updates;
				_updates = new List<ServiceStatisticUpdate>();
			}

			var aggregates = updates
				.GroupBy(x => new {x.TimeSegment, x.ServiceName})
				.Select(x => x.Aggregate((a, b) => new ServiceStatisticUpdate
					{
						ServiceName = x.Key.ServiceName,
						TimeSegment = x.Key.TimeSegment,
						TotalCpuTime = a.TotalCpuTime + b.TotalCpuTime,
						UserCpuTime = a.UserCpuTime + b.UserCpuTime,
						AbsoluteTime = a.AbsoluteTime + b.AbsoluteTime,
						MaxAbsoluteTime = a.MaxAbsoluteTime > b.MaxAbsoluteTime ? a.MaxAbsoluteTime : b.MaxAbsoluteTime,
						TimeStamp = a.TimeStamp > b.TimeStamp ? a.TimeStamp : b.TimeStamp,
						Count = a.Count + b.Count
					}));

			foreach(var aggregate in aggregates)
			{
				Update(aggregate);
			}
		}

		/// <summary>
		/// Remove statistics older than the provided time stamp.
		/// </summary>
		public void RemoveStatisticsBefore(DateTimeOffset before)
		{
			_repository.RemoveServiceStatistics(TimeSegments.DayPrefix, TimeSegments.Day(before));
			_repository.RemoveServiceStatistics(TimeSegments.MonthPrefix, TimeSegments.Month(before));
		}

		/// <summary>
		/// Remove statistics older than the provided number of periods (0 removes all but the current period).
		/// </summary>
		public void RemoveStatisticsBefore(int numberOfPeriods)
		{
			var now = DateTimeOffset.UtcNow;

			_repository.RemoveServiceStatistics(
				TimeSegments.DayPrefix,
				TimeSegments.Day(now.AddDays(-numberOfPeriods)));

			_repository.RemoveServiceStatistics(
				TimeSegments.MonthPrefix,
				TimeSegments.Month(now.AddMonths(-numberOfPeriods)));
		}

		public IDisposable Monitor(CloudService service)
		{
			var handle = OnStart(service);
			return new DisposableAction(() => OnStop(handle));
		}

		RunningServiceHandle OnStart(CloudService service)
		{
			var process = Process.GetCurrentProcess();
			var handle = new RunningServiceHandle
				{
					Service = service,
					TotalProcessorTime = process.TotalProcessorTime,
					UserProcessorTime = process.UserProcessorTime,
					StartDate = DateTimeOffset.UtcNow
				};

			return handle;
		}

		void OnStop(RunningServiceHandle handle)
		{
			var timestamp = DateTimeOffset.UtcNow;
			var process = Process.GetCurrentProcess();
			var serviceName = handle.Service.Name;
			var totalCpuTime = process.TotalProcessorTime - handle.TotalProcessorTime;
			var userCpuTime = process.UserProcessorTime - handle.UserProcessorTime;
			var absoluteTime = timestamp - handle.StartDate;

			lock (_updatesSync)
			{
				_updates.Add(new ServiceStatisticUpdate
					{
						TimeSegment = TimeSegments.Day(timestamp),
						ServiceName = serviceName,
						TimeStamp = timestamp,
						TotalCpuTime = totalCpuTime,
						UserCpuTime = userCpuTime,
						AbsoluteTime = absoluteTime,
						MaxAbsoluteTime = absoluteTime,
						Count = 1
					});

				_updates.Add(new ServiceStatisticUpdate
					{
						TimeSegment = TimeSegments.Month(timestamp),
						ServiceName = serviceName,
						TimeStamp = timestamp,
						TotalCpuTime = totalCpuTime,
						UserCpuTime = userCpuTime,
						AbsoluteTime = absoluteTime,
						MaxAbsoluteTime = absoluteTime,
						Count = 1
					});
			}
		}

		void Update(ServiceStatisticUpdate update)
		{
			_repository.UpdateServiceStatistics(
				update.TimeSegment,
				update.ServiceName,
				s =>
				{
					if (!s.HasValue)
					{
						var now = DateTimeOffset.UtcNow;

						return new ServiceStatistics
						{
							Name = update.ServiceName,
							FirstStartTime = now,
							LastUpdate = now,
							TotalProcessorTime = update.TotalCpuTime,
							UserProcessorTime = update.UserCpuTime,
							AbsoluteTime = update.AbsoluteTime,
							MaxAbsoluteTime = update.AbsoluteTime,
							Count = 1
						};
					}

					var stats = s.Value;
					stats.TotalProcessorTime += update.TotalCpuTime;
					stats.UserProcessorTime += update.UserCpuTime;
					stats.AbsoluteTime += update.AbsoluteTime;

					if (stats.MaxAbsoluteTime < update.AbsoluteTime)
					{
						stats.MaxAbsoluteTime = update.AbsoluteTime;
					}

					stats.LastUpdate = update.TimeStamp;
					stats.Count += update.Count;
					
					return stats;
				});
		}

		private class RunningServiceHandle
		{
			public CloudService Service { get; set; }
			public TimeSpan TotalProcessorTime { get; set; }
			public TimeSpan UserProcessorTime { get; set; }
			public DateTimeOffset StartDate { get; set; }
		}

		private class ServiceStatisticUpdate
		{
			public string ServiceName { get; set; }
			public string TimeSegment { get; set; }

			public DateTimeOffset TimeStamp { get; set; }
			public TimeSpan TotalCpuTime { get; set; }
			public TimeSpan UserCpuTime { get; set; }
			public TimeSpan AbsoluteTime { get; set; }

			public TimeSpan MaxAbsoluteTime { get; set; }
			public long Count { get; set; }
		}
	}
}
