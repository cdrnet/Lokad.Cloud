#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Service Monitoring Statistics
	/// </summary>
	[Serializable]
	public class ServiceStatistics
	{
		public string Name { get; set; }

		public DateTimeOffset FirstStartTime { get; set; }
		public DateTimeOffset LastUpdate { get; set; }

		public TimeSpan TotalProcessorTime { get; set; }
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
