#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Services
{
	/// <summary>
	/// Removes monitoring statistics after a retention period of 24 segments each.
	/// </summary>
	[ScheduledServiceSettings(
		   AutoStart = true,
		   TriggerInterval = 6 * 60 * 60, // 1 execution every 6 hours
		   Description = "Removes old monitoring statistics.")] 
	public class MonitoringDataRetentionService : ScheduledService
	{
		readonly DiagnosticsAcquisition _diagnosticsAcquisition;

		public MonitoringDataRetentionService(DiagnosticsAcquisition diagnosticsAcquisition)
		{
			_diagnosticsAcquisition = diagnosticsAcquisition;
		}

		/// <summary>Called by the framework.</summary>
		protected override void StartOnSchedule()
		{
			_diagnosticsAcquisition.RemoveStatisticsBefore(24);
		}
	}
}
