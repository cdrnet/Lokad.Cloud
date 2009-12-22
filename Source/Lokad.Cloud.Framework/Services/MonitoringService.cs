#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Diagnostics.Persistence;

namespace Lokad.Cloud.Services
{
	/// <summary>
	/// Collects and persists monitoring statistics.
	/// </summary>
	[ScheduledServiceSettings(
		   AutoStart = true,
		   Description = "Collects and persists monitoring statistics.",
		   TriggerInterval = 5 * 60, // 1 execution every 5min
		   SchedulePerWorker = true)] 
	public class MonitoringService : ScheduledService
	{
		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public ICloudDiagnosticsRepository DiagnosticsRepository { get; set; }

		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public PartitionMonitor PartitionMonitor { get; set; }

		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public ExecutionProfilingMonitor ExecutionProfiling { get; set; }

		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public ExceptionTrackingMonitor ExceptionTracking { get; set; }

		/// <summary>Called by the framework.</summary>
		protected override void StartOnSchedule()
		{
			if (DiagnosticsRepository == null)
			{
				DiagnosticsRepository = new BlobDiagnosticsRepository(BlobStorage);
			}

			if (PartitionMonitor == null)
			{
				PartitionMonitor = new PartitionMonitor(DiagnosticsRepository);
			}
			PartitionMonitor.UpdateStatistics();

			if (ExecutionProfiling == null)
			{
				ExecutionProfiling = new ExecutionProfilingMonitor(DiagnosticsRepository);
			}
			ExecutionProfiling.UpdateStatistics();

			if (ExceptionTracking == null)
			{
				ExceptionTracking = new ExceptionTrackingMonitor(DiagnosticsRepository);
			}
			ExceptionTracking.UpdateStatistics();
		}
	}
}
