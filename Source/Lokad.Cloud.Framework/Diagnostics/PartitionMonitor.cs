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
			var statistics = CollectStatistics();
			var timestamp = DateTime.UtcNow;

			_repository.SetPartitionStatistics(TimeSegments.Day(timestamp), _partitionKey, statistics);
			_repository.SetPartitionStatistics(TimeSegments.Month(timestamp), _partitionKey, statistics);
		}

		PartitionStatistics CollectStatistics()
		{
			Process process = Process.GetCurrentProcess();

			return new PartitionStatistics
				{
					PartitionKey = _partitionKey,

					StartTime = process.StartTime,
					LastUpdate = DateTimeOffset.Now,

					TotalProcessorTime = process.TotalProcessorTime,
					UserProcessorTime = process.UserProcessorTime,

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
	}
}
