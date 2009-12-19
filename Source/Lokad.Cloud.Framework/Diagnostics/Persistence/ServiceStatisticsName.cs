#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ServiceStatisticsName : BaseTypedBlobName<ServiceStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-service"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string ServiceName;

		public ServiceStatisticsName(string timeSegment, string serviceName)
		{
			TimeSegment = timeSegment;
			ServiceName = serviceName;
		}

		public static ServiceStatisticsName New(string timeSegment, string serviceName)
		{
			return new ServiceStatisticsName(timeSegment, serviceName);
		}

		public static BlobNamePrefix<ServiceStatisticsName> GetPrefix()
		{
			return GetPrefix(new ServiceStatisticsName(null, null), 0);
		}

		public static BlobNamePrefix<ServiceStatisticsName> GetPrefix(string timeSegment)
		{
			return GetPrefix(new ServiceStatisticsName(timeSegment, null), 1);
		}
	}
}