#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class PartitionStatisticsName : BlobName<PartitionStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-partition"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string PartitionKey;

		public PartitionStatisticsName(string timeSegment, string partitionKey)
		{
			TimeSegment = timeSegment;
			PartitionKey = partitionKey;
		}

		public static PartitionStatisticsName New(string timeSegment, string partitionKey)
		{
			return new PartitionStatisticsName(timeSegment, partitionKey);
		}

        public static PartitionStatisticsName GetPrefix()
		{
			return new PartitionStatisticsName(null, null);
		}

        public static PartitionStatisticsName GetPrefix(string timeSegment)
		{
			return new PartitionStatisticsName(timeSegment, null);
		}
	}
}