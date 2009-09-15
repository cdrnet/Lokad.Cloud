#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Threading;

namespace Lokad.Cloud.Azure
{
	/// <summary>Helper class to deal with pathological situations where
	/// a worker crashes at start-up time (typically because initialization
	/// or assembly loading goes wrong). Instead of performing a high-frequency
	/// restart (producing junk logs among other), when restart flood is detected
	/// restarts are forcefully slowed down.</summary>
	public class NoRestartFloodPolicy
	{
		/// <summary>Minimal duration between worker restart to be considered
		/// as a regular situation (restart can happen from time to time).</summary>
		static TimeSpan FloodFrequencyThreshold { get { return 1.Minutes(); } }

		/// <summary>Delay to be applied before the next restart when a
		/// flooding situation is detected.</summary>
		static TimeSpan DelayWhenFlooding { get { return 5.Minutes(); } }

		Action<bool> _isRestartFlooding;

		public NoRestartFloodPolicy(Action<bool> isRestartFlooding)
		{
			_isRestartFlooding = isRestartFlooding;
		}

		/// <summary>Endlessly restart the provided action, but
		/// avoiding restart flooding patterns.</summary>
		public void Do(Action workButNotFloodRestart)
		{
			while(true)
			{
				var lastRestart = DateTime.UtcNow;
				workButNotFloodRestart();

				if(DateTime.UtcNow.Subtract(lastRestart) < FloodFrequencyThreshold)
				{
					_isRestartFlooding(true);
					Thread.Sleep(DelayWhenFlooding);
				}

				_isRestartFlooding(false);
			}
		}
	}
}
