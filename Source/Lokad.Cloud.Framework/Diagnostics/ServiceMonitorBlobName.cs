using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics
{
	internal class ServiceMonitorBlobName : BaseTypedBlobName<ServiceMonitorPartitionData>
	{
		public override string ContainerName
		{
			get { return "lokad-monitoring"; }
		}

		[UsedImplicitly, Rank(0)] public readonly string PartitionKey;

		public ServiceMonitorBlobName(string partitionKey)
		{
			PartitionKey = partitionKey;
		}

		public static ServiceMonitorBlobName New(string partitionKey)
		{
			return new ServiceMonitorBlobName(partitionKey);
		}

		public static BlobNamePrefix<ServiceMonitorBlobName> GetPrefix()
		{
			return GetPrefix(new ServiceMonitorBlobName(null), 0);
		}
	}

	[Serializable]
	public class ServiceMonitorPartitionData
	{
		public string PartitionKey { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public DateTimeOffset LastUpdate { get; set; }

		public int HandleCount { get; set; }
		public int ThreadCount { get; set; }

		public TimeSpan TotalProcessorTime { get; set; }
		public TimeSpan UserProcessorTime { get; set; }

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

		public Dictionary<string, ServiceMonitorServiceData> Services { get; set; }
	}

	[Serializable]
	public class ServiceMonitorServiceData
	{
		public string Name { get; set; }

		public TimeSpan TotalProcessorTime { get; set; }
		public TimeSpan UserProcessorTime { get; set; }
	}
}
