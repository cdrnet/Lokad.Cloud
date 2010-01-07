#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Diagnostics;
using Lokad.Cloud.Azure;

// TODO: Discard old data (based on .LastUpdate)

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Partition and Worker Monitoring Data Provider
	/// </summary>
	internal class PartitionMonitor
	{
		readonly ICloudDiagnosticsRepository _repository;
		readonly string _partitionKey;
		readonly string _instanceId;

		/// <summary>
		/// Creates an instance of the <see cref="PartitionMonitor"/> class.
		/// </summary>
		public PartitionMonitor(ICloudDiagnosticsRepository repository)
		{
			_repository = repository;
			_partitionKey = CloudEnvironment.PartitionKey;
			_instanceId = CloudEnvironment.PartitionInstanceId.GetValue("N/A");
		}

		public void UpdateStatistics()
		{
			var process = Process.GetCurrentProcess();
			var timestamp = DateTimeOffset.Now;

			UpdateStatistics(TimeSegments.Day(timestamp), process);
			UpdateStatistics(TimeSegments.Month(timestamp), process);
		}

		void UpdateStatistics(string timeSegment, Process process)
		{
			_repository.UpdatePartitionStatistics(
				timeSegment,
				_partitionKey,
				s =>
					{
						var now = DateTimeOffset.Now;

						if (!s.HasValue)
						{
							return new PartitionStatistics
								{
									// WORKER DETAILS
									PartitionKey = _partitionKey,
									InstanceId = _instanceId,
									OperatingSystem = Environment.OSVersion.ToString(),
									Runtime = Environment.Version.ToString(),
									ProcessorCount = Environment.ProcessorCount,

									// WORKER AVAILABILITY
									StartTime = process.StartTime,
									StartCount = 0,
									LastUpdate = now,
									ActiveTime = new TimeSpan(),
									LifetimeActiveTime = now - process.StartTime,

									// THREADS & HANDLES
									HandleCount = process.HandleCount,
									ThreadCount = process.Threads.Count,

									// CPU PROCESSING
									TotalProcessorTime = new TimeSpan(),
									UserProcessorTime = new TimeSpan(),
									LifetimeTotalProcessorTime = process.TotalProcessorTime,
									LifetimeUserProcessorTime = process.UserProcessorTime,

									// MEMORY CONSUMPTION
									MemorySystemNonPagedSize = process.NonpagedSystemMemorySize64,
									MemorySystemPagedSize = process.PagedSystemMemorySize64,
									MemoryVirtualPeakSize = process.PeakVirtualMemorySize64,
									MemoryWorkingSet = process.WorkingSet64,
									MemoryWorkingSetPeak = process.PeakWorkingSet64,
									MemoryPrivateSize = process.PrivateMemorySize64,
									MemoryPagedSize = process.PagedMemorySize64,
									MemoryPagedPeakSize = process.PeakPagedMemorySize64,

								};
						}

						var stats = s.Value;

						// WORKER DETAILS
						stats.InstanceId = _instanceId;
						stats.OperatingSystem = Environment.OSVersion.ToString();
						stats.Runtime = Environment.Version.ToString();
						stats.ProcessorCount = Environment.ProcessorCount;

						// WORKER AVAILABILITY
						var wasRestarted = false;
						if (process.StartTime > stats.StartTime)
						{
							wasRestarted = true;
							stats.StartCount++;
							stats.StartTime = process.StartTime;
						}

						stats.LastUpdate = now;

						if (stats.LifetimeActiveTime.Ticks == 0)
						{
							// Upgrade old data structures
							stats.ActiveTime = new TimeSpan();
						}
						else if (wasRestarted)
						{
							stats.ActiveTime += now - process.StartTime;
						}
						else
						{
							stats.ActiveTime += (now - process.StartTime) - stats.LifetimeActiveTime;
						}
						stats.LifetimeActiveTime = now - process.StartTime;

						// THREADS & HANDLES
						stats.HandleCount = process.HandleCount;
						stats.ThreadCount = process.Threads.Count;

						// CPU PROCESSING
						if (stats.LifetimeTotalProcessorTime.Ticks == 0)
						{
							// Upgrade old data structures
							stats.TotalProcessorTime = new TimeSpan();
							stats.UserProcessorTime = new TimeSpan();
						}
						else if(wasRestarted)
						{
							stats.TotalProcessorTime += process.TotalProcessorTime;
							stats.UserProcessorTime += process.UserProcessorTime;
						}
						else
						{
							stats.TotalProcessorTime += process.TotalProcessorTime - stats.LifetimeTotalProcessorTime;
							stats.UserProcessorTime += process.UserProcessorTime - stats.LifetimeUserProcessorTime;
						}
						stats.LifetimeTotalProcessorTime = process.TotalProcessorTime;
						stats.LifetimeUserProcessorTime = process.UserProcessorTime;

						// MEMORY CONSUMPTION
						stats.MemorySystemNonPagedSize = process.NonpagedSystemMemorySize64;
						stats.MemorySystemPagedSize = process.PagedSystemMemorySize64;
						stats.MemoryVirtualPeakSize = process.PeakVirtualMemorySize64;
						stats.MemoryWorkingSet = process.WorkingSet64;
						stats.MemoryWorkingSetPeak = process.PeakWorkingSet64;
						stats.MemoryPrivateSize = process.PrivateMemorySize64;
						stats.MemoryPagedSize = process.PagedMemorySize64;
						stats.MemoryPagedPeakSize = process.PeakPagedMemorySize64;

						return stats;
					});
		}
	}
}
