#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>
	/// The execution result of a scheduled action, providing information that
	/// might be considered for further scheduling.
	/// </summary>
	public enum ScheduleResult
	{
		/// <summary>
		/// No information available or the service is not interested in providing
		/// any details.
		/// </summary>
		DontCare = 0,

		/// <summary>
		/// The service knows or assumes that there is more work available.
		/// </summary>
		WorkAvailable,

		/// <summary>
		/// The service did some work, but knows or assumes that there is no more work
		/// available.
		/// </summary>
		DoneForNow,

		/// <summary>
		/// The service skipped without doing any work (and expects the same for
		/// successive calls).
		/// </summary>
		Skipped
	}

	/// <summary>
	/// Round robin scheduler with modifications: tasks that claim to have more
	/// work ready are given the chance to continue until they reach a fixed time
	/// limit (greedy), and the scheduling is slowed down when all available
	/// services skip execution consecutively.
	/// </summary>
	internal class Scheduler
	{
		readonly Func<IEnumerable<CloudService>> _serviceProvider;
		readonly Func<CloudService, ScheduleResult> _schedule;
		readonly object _sync = new object();

		/// <summary>Duration to keep pinging the same cloud service if service is active.</summary>
		readonly TimeSpan _moreOfTheSame = 60.Seconds();

		/// <summary>Resting duration.</summary>
		readonly TimeSpan _idleSleep = 10.Seconds();

		CloudService _currentService;
		bool _isRunning;

		/// <summary>
		/// Creates a new instance of the Scheduler class.
		/// </summary>
		/// <param name="serviceProvider">Provider of available cloud services</param>
		/// <param name="schedule">Action to be invoked when a service is scheduled to run</param>
		public Scheduler(Func<IEnumerable<CloudService>> serviceProvider, Func<CloudService, ScheduleResult> schedule)
		{
			_serviceProvider = serviceProvider;
			_schedule = schedule;
		}

		public CloudService CurrentlyScheduledService
		{
			get { return _currentService; }
		}

		public IEnumerable<Action> Schedule()
		{
			var services = _serviceProvider().ToArray();
			var currentServiceIndex = -1;
			var skippedConsecutively = 0;

			_isRunning = true;

			while (_isRunning)
			{
				currentServiceIndex = (currentServiceIndex + 1) % services.Length;
				_currentService = services[currentServiceIndex];

				var result = ScheduleResult.DontCare;
				var isRunOnce = false;

				// 'more of the same pattern'
				// as long the service is active, keep triggering the same service
				// for at least 1min (in order to avoid a single service to monopolize CPU)
				var start = DateTimeOffset.Now;
				while (DateTimeOffset.Now.Subtract(start) < _moreOfTheSame && (result == ScheduleResult.WorkAvailable || result == ScheduleResult.DontCare))
				{
					yield return () => { result = _schedule(_currentService); };
					isRunOnce |= result != ScheduleResult.Skipped;
				}

				skippedConsecutively = isRunOnce ? 0 : skippedConsecutively + 1;
				if (skippedConsecutively >= services.Length)
				{
					// We are not using 'Thread.Sleep' because we want the worker
					// to terminate fast if 'Stop' is requested.
					lock (_sync)
					{
						Monitor.Wait(_sync, _idleSleep);
					}

					skippedConsecutively = 0;
				}
			}

			_currentService = null;
		}

		public void AbortWaitingSchedule()
		{
			_isRunning = false;
			lock (_sync)
			{
				Monitor.Pulse(_sync);
			}
		}
	}
}
