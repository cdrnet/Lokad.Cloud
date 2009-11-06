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
		   TriggerInterval = 5*60)] // 1 execution every 5min
	public class MonitoringService : ScheduledService
	{
		/// <summary>Called by the framework.</summary>
		protected override void StartOnSchedule()
		{
			// Update Partition Statistics
			var partitionMonitor = new PartitionMonitor(BlobStorage);
			partitionMonitor.UpdateStatistics();

			// TODO: Update data from other monitoring sources as well
			// (provide some way to inject them)
		}
	}
}
