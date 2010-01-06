#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Diagnostics;

// TODO: Discard old data (based on .LastUpdate)

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Service Monitoring Data Provider
	/// </summary>
	internal class ServiceMonitor : IServiceMonitor
	{
		readonly ICloudDiagnosticsRepository _repository;

		/// <summary>
		/// Creates an instance of the <see cref="ServiceMonitor"/> class.
		/// </summary>
		public ServiceMonitor(ICloudDiagnosticsRepository repository)
		{
			_repository = repository;
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
					UserProcessorTime = process.UserProcessorTime
				};

			return handle;
		}

		void OnStop(RunningServiceHandle handle)
		{
			var serviceName = handle.Service.Name;
			var process = Process.GetCurrentProcess();
			var timestamp = DateTimeOffset.Now;

			UpdateStatistics(TimeSegments.Day(timestamp), serviceName, handle, process);
            UpdateStatistics(TimeSegments.Month(timestamp), serviceName, handle, process);
		}

		void UpdateStatistics(string timeSegment, string serviceName, RunningServiceHandle handle, Process process)
		{
			_repository.UpdateServiceStatistics(
				timeSegment,
				serviceName,
				s =>
				{
					if (!s.HasValue)
					{
						return new ServiceStatistics
						{
							Name = serviceName,
							FirstStartTime = DateTimeOffset.Now,
							LastUpdate = DateTimeOffset.Now,
							TotalProcessorTime = process.TotalProcessorTime - handle.TotalProcessorTime,
							UserProcessorTime = process.UserProcessorTime - handle.UserProcessorTime
						};
					}

					var stats = s.Value;
					stats.TotalProcessorTime += process.TotalProcessorTime - handle.TotalProcessorTime;
					stats.UserProcessorTime += process.UserProcessorTime - handle.UserProcessorTime;
					stats.LastUpdate = DateTimeOffset.Now;
					return stats;
				});
		}

		private class RunningServiceHandle
		{
			public CloudService Service { get; set; }
			public TimeSpan TotalProcessorTime { get; set; }
			public TimeSpan UserProcessorTime { get; set; }
		}
	}
}
