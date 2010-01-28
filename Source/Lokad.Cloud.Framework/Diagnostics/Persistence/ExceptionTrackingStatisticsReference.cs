#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ExceptionTrackingStatisticsReference : BlobReference<ExceptionTrackingStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-exception"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string ContextName;

		public ExceptionTrackingStatisticsReference(string timeSegment, string contextName)
		{
			TimeSegment = timeSegment;
			ContextName = contextName;
		}

		public static ExceptionTrackingStatisticsReference New(string timeSegment, string contextName)
		{
			return new ExceptionTrackingStatisticsReference(timeSegment, contextName);
		}

		public static BlobNamePrefix<ExceptionTrackingStatisticsReference> GetPrefix()
		{
			return GetPrefix(new ExceptionTrackingStatisticsReference(null, null), 0);
		}

		public static BlobNamePrefix<ExceptionTrackingStatisticsReference> GetPrefix(string timeSegment)
		{
			return GetPrefix(new ExceptionTrackingStatisticsReference(timeSegment, null), 1);
		}
	}
}
