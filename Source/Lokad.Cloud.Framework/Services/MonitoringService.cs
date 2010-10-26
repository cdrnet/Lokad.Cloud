#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Services
{
	/// <summary>
	/// Collects and persists monitoring statistics.
	/// </summary>
	[ScheduledServiceSettings(
		   AutoStart = true,
		   TriggerInterval = 2 * 60, // 1 execution every 2min
		   SchedulePerWorker = true,
		   Description = "Collects and persists monitoring statistics.")] 
	public class MonitoringService : ScheduledService
	{
		readonly DiagnosticsAcquisition _diagnosticsAcquisition;

		public MonitoringService(DiagnosticsAcquisition diagnosticsAcquisition)
		{
			_diagnosticsAcquisition = diagnosticsAcquisition;
		}

		/// <summary>Called by the framework.</summary>
		protected override void StartOnSchedule()
		{
			_diagnosticsAcquisition.CollectStatistics();
		}
	}
}
