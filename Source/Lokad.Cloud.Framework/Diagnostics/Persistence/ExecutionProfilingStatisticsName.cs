#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Storage.Blobs;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	internal class ExecutionProfilingStatisticsName : BlobName<ExecutionProfilingStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-profile"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string TimeSegment;

		[UsedImplicitly, Rank(1)]
		public readonly string ContextName;

		public ExecutionProfilingStatisticsName(string timeSegment, string contextName)
		{
			TimeSegment = timeSegment;
			ContextName = contextName;
		}

		public static ExecutionProfilingStatisticsName New(string timeSegment, string contextName)
		{
			return new ExecutionProfilingStatisticsName(timeSegment, contextName);
		}

		public static ExecutionProfilingStatisticsName GetPrefix()
		{
			return new ExecutionProfilingStatisticsName(null, null);
		}

		public static ExecutionProfilingStatisticsName GetPrefix(string timeSegment)
		{
			return new ExecutionProfilingStatisticsName(timeSegment, null);
		}
	}
}