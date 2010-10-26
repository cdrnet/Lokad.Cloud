#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Service Monitoring Statistics
	/// </summary>
	[Serializable]
	[DataContract]
	public class ServiceStatistics
	{
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public DateTimeOffset FirstStartTime { get; set; }
		[DataMember]
		public DateTimeOffset LastUpdate { get; set; }

		[DataMember]
		public TimeSpan TotalProcessorTime { get; set; }
		[DataMember]
		public TimeSpan UserProcessorTime { get; set; }

		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan AbsoluteTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public TimeSpan MaxAbsoluteTime { get; set; }
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public long Count { get; set; }
	}
}
