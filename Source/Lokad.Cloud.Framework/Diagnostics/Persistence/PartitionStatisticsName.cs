#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class PartitionStatisticsName : BaseTypedBlobName<PartitionStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-partition"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string PartitionKey;

		public PartitionStatisticsName(string partitionKey)
		{
			PartitionKey = partitionKey;
		}

		public static PartitionStatisticsName New(string partitionKey)
		{
			return new PartitionStatisticsName(partitionKey);
		}

		public static BlobNamePrefix<PartitionStatisticsName> GetPrefix()
		{
			return GetPrefix(new PartitionStatisticsName(null), 0);
		}
	}
}