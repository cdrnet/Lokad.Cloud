#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>
	/// Helper class to deal with pathological situations where a worker crashes at
	/// start-up time (typically because initialization or assembly loading goes
	/// wrong). Instead of performing a high-frequency restart (producing junk logs
	/// among other), when restart flood is detected restarts are forcefully slowed
	/// down.
	/// </summary>
	internal class NoRestartFloodPolicy
	{
		/// <summary>
		/// Minimal duration between worker restart to be considered as a regular
		/// situation (restart can happen from time to time).
		/// </summary>
		static TimeSpan FloodFrequencyThreshold { get { return 1.Minutes(); } }

		/// <summary>
		/// Delay to be applied before the next restart when a flooding situation is
		/// detected.
		/// </summary>
		static TimeSpan DelayWhenFlooding { get { return 5.Minutes(); } }

		volatile bool _isStopRequested;

		/// <summary>When stop is requested, policy won't go on with restarts anymore.</summary>
		public bool IsStopRequested
		{
			get { return _isStopRequested; }
			set { _isStopRequested = value; }
		}

		/// <summary>
		/// Endlessly restart the provided action, but avoiding restart flooding
		/// patterns.
		/// </summary>
		public void Do(Func<bool> workButNotFloodRestart)
		{
			// once stop is requested, we stop
			while (!_isStopRequested)
			{
				// The assemblyUpdated flag handles the case when a restart is caused by an asm update, "soon" after another restart
				// In such case, the worker would be reported as unhealthy virtually forever if no more restarts occur

				var lastRestart = DateTimeOffset.UtcNow;
				var assemblyUpdated = workButNotFloodRestart();

				if (!assemblyUpdated && DateTimeOffset.UtcNow.Subtract(lastRestart) < FloodFrequencyThreshold)
				{
					// Unhealthy
					Thread.Sleep(DelayWhenFlooding);
				}
			}
		}
	}
}