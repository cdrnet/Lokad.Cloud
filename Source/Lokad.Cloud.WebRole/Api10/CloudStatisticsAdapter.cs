#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Quality;

namespace Lokad.Cloud.Web.Api10
{
	[UsedImplicitly]
	public class CloudStatisticsAdapter : CloudStatistics
	{
		public CloudStatisticsAdapter()
			: base(GlobalSetup.Container.Resolve<ICloudDiagnosticsRepository>())
		{
		}
	}
}