#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Partition & Worker Monitoring Statistics
	/// </summary>
	[Serializable]
	public class PartitionStatistics
	{
		public string PartitionKey { get; set; }

		public DateTimeOffset StartTime { get; set; }
		public DateTimeOffset LastUpdate { get; set; }

		public TimeSpan TotalProcessorTime { get; set; }
		public TimeSpan UserProcessorTime { get; set; }

		public int HandleCount { get; set; }
		public int ThreadCount { get; set; }

		public long MemorySystemNonPagedSize { get; set; }
		public long MemorySystemPagedSize { get; set; }
		public long MemoryVirtualPeakSize { get; set; }
		public long MemoryWorkingSet { get; set; }
		public long MemoryWorkingSetPeak { get; set; }
		public long MemoryPrivateSize { get; set; }
		public long MemoryPagedSize { get; set; }
		public long MemoryPagedPeakSize { get; set; }

		public string Runtime { get; set; }
		public string OperatingSystem { get; set; }
		public int ProcessorCount { get; set; }
	}

	internal class PartitionStatisticsName : BaseTypedBlobName<PartitionStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-partition"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string PartitionKey;

		public PartitionStatisticsName(string partitionKey)
		{
			PartitionKey = partitionKey;
		}

		public static PartitionStatisticsName New(string partitionKey)
		{
			return new PartitionStatisticsName(partitionKey);
		}

		public static BlobNamePrefix<PartitionStatisticsName> GetPrefix()
		{
			return GetPrefix(new PartitionStatisticsName(null), 0);
		}
	}
}
