#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage;
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

		public static ExceptionTrackingStatisticsReference GetPrefix()
		{
			return new ExceptionTrackingStatisticsReference(null, null);
		}

        public static ExceptionTrackingStatisticsReference GetPrefix(string timeSegment)
		{
			return new ExceptionTrackingStatisticsReference(timeSegment, null);
		}
	}
}
