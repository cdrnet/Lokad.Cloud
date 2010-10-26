#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Partition and Worker Monitoring Statistics
	/// </summary>
	/// <remarks>
	/// Properties prefixed with Lifetime refer to the lifetime of the partition's
	/// process. Is a process restarted, the lifetime value will be reset to zero.
	/// These additional values are needed internally in order to compute the
	/// actual non-lifetime values.
	/// </remarks>
	[Serializable]
	[DataContract]
	public class PartitionStatistics
	{
		// WORKER DETAILS

		[DataMember]
		public string PartitionKey { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public string InstanceId { get; set; }
		[DataMember]
		public string OperatingSystem { get; set; }
		[DataMember]
		public string Runtime { get; set; }
		[DataMember]
		public int ProcessorCount { get; set; }

		// WORKER AVAILABILITY

		[DataMember]
		public DateTimeOffset StartTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public int StartCount { get; set; }
		[DataMember]
		public DateTimeOffset LastUpdate { get; set; }

		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan ActiveTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan LifetimeActiveTime { get; set; }

		// THREADS & HANDLES

		[DataMember]
		public int HandleCount { get; set; }
		[DataMember]
		public int ThreadCount { get; set; }

		// CPU PROCESSING

		[DataMember]
		public TimeSpan TotalProcessorTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan LifetimeTotalProcessorTime { get; set; }

		[DataMember]
		public TimeSpan UserProcessorTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan LifetimeUserProcessorTime { get; set; }

		// MEMORY CONSUMPTION

		[DataMember]
		public long MemorySystemNonPagedSize { get; set; }
		[DataMember]
		public long MemorySystemPagedSize { get; set; }
		[DataMember]
		public long MemoryVirtualPeakSize { get; set; }
		[DataMember]
		public long MemoryWorkingSet { get; set; }
		[DataMember]
		public long MemoryWorkingSetPeak { get; set; }
		[DataMember]
		public long MemoryPrivateSize { get; set; }
		[DataMember]
		public long MemoryPagedSize { get; set; }
		[DataMember]
		public long MemoryPagedPeakSize { get; set; }
	}
}
