#region Copyright (c) Lokad 2009
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
		   Description = "Collects and persists monitoring statistics.",
		   TriggerInterval = 5 * 60)] // 1 execution every 5min
	public class MonitoringService : ScheduledService
	{
		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public PartitionMonitor PartitionMonitor { get; set; }

		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public ExecutionProfilingMonitor ExecutionProfiling { get; set; }

		/// <remarks>IoC Injected (optional, failover to default)</remarks>
		public ExceptionTrackingMonitor ExceptionTracking { get; set; }

		/// <summary>Called by the framework.</summary>
		protected override void StartOnSchedule()
		{
			if (PartitionMonitor == null)
			{
				PartitionMonitor = new PartitionMonitor(BlobStorage);
			}
			PartitionMonitor.UpdateStatistics();

			if (ExecutionProfiling == null)
			{
				ExecutionProfiling = new ExecutionProfilingMonitor(BlobStorage);
			}
			ExecutionProfiling.UpdateStatistics();

			if (ExceptionTracking == null)
			{
				ExceptionTracking = new ExceptionTrackingMonitor(BlobStorage);
			}
			ExceptionTracking.UpdateStatistics();
		}
	}
}
