#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.ServiceFabric
{
	/// <summary>Schedule settings for the execution of a <see cref="ScheduledService"/>.</summary>
	/// <remarks>The implementation is kept very simple for now. Complete scheduling,
	/// specifying specific hours or days will be added later on.</remarks>
	public sealed class ScheduledServiceSettingsAttribute : CloudServiceSettingsAttribute
	{
		/// <summary>Indicates the interval between the scheduled executions
		/// (expressed in seconds).</summary>
		/// <remarks><c>TimeSpan</c> cannot be used here, because it's not compatible
		/// with the attribute usage.</remarks>
		public double TriggerInterval { get; set; }

		/// <summary>
		/// Indicates whether the service is scheduled globally among all cloud
		/// workers (default, <c>false</c>), or whether it should be scheduled
		/// separately per worker. If scheduled per worker, the service will
		/// effectively run the number of cloud worker instances times the normal
		/// rate, and can run on multiple workers at the same time.
		/// </summary>
		public bool SchedulePerWorker { get; set; }
	}
}