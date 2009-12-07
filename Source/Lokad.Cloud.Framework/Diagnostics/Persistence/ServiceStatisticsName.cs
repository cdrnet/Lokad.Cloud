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