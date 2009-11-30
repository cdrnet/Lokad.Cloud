#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Quality;

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
	}

	internal class ServiceStatisticsName : BaseTypedBlobName<ServiceStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-service"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string ServiceName;

		public ServiceStatisticsName(string serviceName)
		{
			ServiceName = serviceName;
		}

		public static ServiceStatisticsName New(string serviceName)
		{
			return new ServiceStatisticsName(serviceName);
		}

		public static BlobNamePrefix<ServiceStatisticsName> GetPrefix()
		{
			return GetPrefix(new ServiceStatisticsName(null), 0);
		}
	}
}
