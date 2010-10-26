#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ServiceStatisticsName : BlobName<ServiceStatistics>
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

        public static ServiceStatisticsName GetPrefix()
		{
			return new ServiceStatisticsName(null, null);
		}

        public static ServiceStatisticsName GetPrefix(string timeSegment)
		{
			return new ServiceStatisticsName(timeSegment, null);
		}
	}
}