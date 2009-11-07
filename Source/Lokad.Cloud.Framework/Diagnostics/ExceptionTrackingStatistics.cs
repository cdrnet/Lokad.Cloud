#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Diagnostics.Persist;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics
{
	[Serializable]
	public class ExceptionTrackingStatistics
	{
		public string Name { get; set; }

		public ExceptionData[] Statistics { get; set; }
	}

	internal class ExceptionTrackingStatisticsName : BaseTypedBlobName<ExceptionTrackingStatistics>
	{
		public override string ContainerName
		{
			get { return "lokad-cloud-diag-exception"; }
		}

		[UsedImplicitly, Rank(0)]
		public readonly string ContextName;

		public ExceptionTrackingStatisticsName(string contextName)
		{
			ContextName = contextName;
		}

		public static ExceptionTrackingStatisticsName New(string contextName)
		{
			return new ExceptionTrackingStatisticsName(contextName);
		}

		public static BlobNamePrefix<ExceptionTrackingStatisticsName> GetPrefix()
		{
			return GetPrefix(new ExceptionTrackingStatisticsName(null), 0);
		}
	}
}
