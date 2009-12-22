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
	/// Cloud Partition & Worker Monitoring Data Provider
	/// </summary>
	public class PartitionMonitor
	{
		readonly ICloudDiagnosticsRepository _repository;
		readonly string _partitionKey;

		/// <summary>
		/// Creates an instance of the <see cref="PartitionMonitor"/> class.
		/// </summary>
		public PartitionMonitor(ICloudDiagnosticsRepository repository)
		{
			_repository = repository;
			_partitionKey = System.Net.Dns.GetHostName();
		}

		public void UpdateStatistics()
		{
			var process = Process.GetCurrentProcess();
			var timestamp = DateTime.UtcNow;

			UpdateStatistics(TimeSegments.Day(timestamp), _partitionKey, process);
			UpdateStatistics(TimeSegments.Month(timestamp), _partitionKey, process);
		}

		void UpdateStatistics(string timeSegment, string partitionName, Process process)
		{
			_repository.UpdatePartitionStatistics(
				timeSegment,
				partitionName,
				s =>
					{
						if (!s.HasValue)
						{
							return new PartitionStatistics
								{
									PartitionKey = partitionName,

									StartTime = process.StartTime,
									LastUpdate = DateTimeOffset.Now,

									TotalProcessorTime = new TimeSpan(),
									UserProcessorTime = new TimeSpan(),
									LifetimeTotalProcessorTime = process.TotalProcessorTime,
									LifetimeUserProcessorTime = process.UserProcessorTime,

									HandleCount = process.HandleCount,
									ThreadCount = process.Threads.Count,

									MemorySystemNonPagedSize = process.NonpagedSystemMemorySize64,
									MemorySystemPagedSize = process.PagedSystemMemorySize64,
									MemoryVirtualPeakSize = process.PeakVirtualMemorySize64,
									MemoryWorkingSet = process.WorkingSet64,
									MemoryWorkingSetPeak = process.PeakWorkingSet64,
									MemoryPrivateSize = process.PrivateMemorySize64,
									MemoryPagedSize = process.PagedMemorySize64,
									MemoryPagedPeakSize = process.PeakPagedMemorySize64,

									Runtime = Environment.Version.ToString(),
									OperatingSystem = Environment.OSVersion.ToString(),
									ProcessorCount = Environment.ProcessorCount
								};
						}

						var stats = s.Value;

						if (stats.LifetimeTotalProcessorTime.Ticks == 0)
						{
							// Upgrade from old format
							stats.TotalProcessorTime = new TimeSpan();
							stats.UserProcessorTime = new TimeSpan();
						}
						else if(process.TotalProcessorTime < stats.LifetimeTotalProcessorTime)
						{
							// Partition restarted in this time segment
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

						stats.HandleCount = process.HandleCount;
						stats.ThreadCount = process.Threads.Count;

						stats.MemorySystemNonPagedSize = process.NonpagedSystemMemorySize64;
						stats.MemorySystemPagedSize = process.PagedSystemMemorySize64;
						stats.MemoryVirtualPeakSize = process.PeakVirtualMemorySize64;
						stats.MemoryWorkingSet = process.WorkingSet64;
						stats.MemoryWorkingSetPeak = process.PeakWorkingSet64;
						stats.MemoryPrivateSize = process.PrivateMemorySize64;
						stats.MemoryPagedSize = process.PagedMemorySize64;
						stats.MemoryPagedPeakSize = process.PeakPagedMemorySize64;

						stats.LastUpdate = DateTimeOffset.Now;
						return stats;
					});
		}
	}
}
