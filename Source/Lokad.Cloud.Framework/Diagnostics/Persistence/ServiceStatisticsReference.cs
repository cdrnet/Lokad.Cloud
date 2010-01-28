#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ServiceStatisticsReference : BlobReference<ServiceStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-service"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string ServiceName;

		public ServiceStatisticsReference(string timeSegment, string serviceName)
		{
			TimeSegment = timeSegment;
			ServiceName = serviceName;
		}

		public static ServiceStatisticsReference New(string timeSegment, string serviceName)
		{
			return new ServiceStatisticsReference(timeSegment, serviceName);
		}

		public static BlobNamePrefix<ServiceStatisticsReference> GetPrefix()
		{
			return GetPrefix(new ServiceStatisticsReference(null, null), 0);
		}

		public static BlobNamePrefix<ServiceStatisticsReference> GetPrefix(string timeSegment)
		{
			return GetPrefix(new ServiceStatisticsReference(timeSegment, null), 1);
		}
	}
}