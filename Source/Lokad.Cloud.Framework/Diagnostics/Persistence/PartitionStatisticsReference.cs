#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class PartitionStatisticsReference : BlobReference<PartitionStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-partition"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string PartitionKey;

		public PartitionStatisticsReference(string timeSegment, string partitionKey)
		{
			TimeSegment = timeSegment;
			PartitionKey = partitionKey;
		}

		public static PartitionStatisticsReference New(string timeSegment, string partitionKey)
		{
			return new PartitionStatisticsReference(timeSegment, partitionKey);
		}

        public static PartitionStatisticsReference GetPrefix()
		{
			return new PartitionStatisticsReference(null, null);
		}

        public static PartitionStatisticsReference GetPrefix(string timeSegment)
		{
			return new PartitionStatisticsReference(timeSegment, null);
		}
	}
}