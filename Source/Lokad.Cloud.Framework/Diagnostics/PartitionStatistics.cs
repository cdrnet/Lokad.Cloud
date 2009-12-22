#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Partition & Worker Monitoring Statistics
	/// </summary>
	[Serializable]
	[DataContract]
	public class PartitionStatistics
	{
		[DataMember]
		public string PartitionKey { get; set; }

		[DataMember]
		public DateTimeOffset StartTime { get; set; }
		[DataMember]
		public DateTimeOffset LastUpdate { get; set; }

		[DataMember]
		public TimeSpan TotalProcessorTime { get; set; }
		[DataMember]
		public TimeSpan UserProcessorTime { get; set; }

		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan LifetimeTotalProcessorTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan LifetimeUserProcessorTime { get; set; }

		[DataMember]
		public int HandleCount { get; set; }
		[DataMember]
		public int ThreadCount { get; set; }

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

		[DataMember]
		public string Runtime { get; set; }
		[DataMember]
		public string OperatingSystem { get; set; }
		[DataMember]
		public int ProcessorCount { get; set; }
	}
}
