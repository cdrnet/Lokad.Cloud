﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Services
{
	/// <summary>
	/// Collects and persists monitoring statistics.
	/// </summary>
	[ScheduledServiceSettings(
		   AutoStart = true,
		   TriggerInterval = 5 * 60, // 1 execution every 5min
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
