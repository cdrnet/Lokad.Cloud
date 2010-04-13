#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ExceptionTrackingStatisticsName : BlobName<ExceptionTrackingStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-exception"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string ContextName;

		public ExceptionTrackingStatisticsName(string timeSegment, string contextName)
		{
			TimeSegment = timeSegment;
			ContextName = contextName;
		}

		public static ExceptionTrackingStatisticsName New(string timeSegment, string contextName)
		{
			return new ExceptionTrackingStatisticsName(timeSegment, contextName);
		}

		public static ExceptionTrackingStatisticsName GetPrefix()
		{
			return new ExceptionTrackingStatisticsName(null, null);
		}

        public static ExceptionTrackingStatisticsName GetPrefix(string timeSegment)
		{
			return new ExceptionTrackingStatisticsName(timeSegment, null);
		}
	}
}
