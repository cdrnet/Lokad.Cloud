#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// TODO: Discard old data (based on .LastUpdate)

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Service Monitoring
	/// </summary>
	public class ServiceMonitor : IServiceMonitor
	{
		readonly IBlobStorageProvider _provider;

		public ServiceMonitor(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		public IEnumerable<ServiceStatistics> GetStatistics()
		{
			return _provider
				.List(ServiceStatisticsName.GetPrefix())
				.Select(name => _provider.GetBlobOrDelete(name))
				.Where(s => null != s);
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
			string serviceName = handle.Service.Name;
			var process = Process.GetCurrentProcess();

			ServiceStatistics result;
			_provider.AtomicUpdate(
				ServiceStatisticsName.New(serviceName),
				stat =>
					{
						if (stat == null)
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

						stat.TotalProcessorTime += process.TotalProcessorTime - handle.TotalProcessorTime;
						stat.UserProcessorTime += process.UserProcessorTime - handle.UserProcessorTime;
						stat.LastUpdate = DateTimeOffset.Now;
						return stat;
					},
				out result
				);
		}

		private class RunningServiceHandle
		{
			public CloudService Service { get; set; }
			public TimeSpan TotalProcessorTime { get; set; }
			public TimeSpan UserProcessorTime { get; set; }
		}
	}
}
