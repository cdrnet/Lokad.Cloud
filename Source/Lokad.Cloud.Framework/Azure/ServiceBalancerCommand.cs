#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Linq;
using System.Threading;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Azure
{
	/// <summary>Organize the executions of the services.</summary>
	public class ServiceBalancerCommand : ICommand
	{
		/// <summary>Resting duration expressed in seconds.</summary>
		const int IdleSleep = 5;

		readonly object _sync = new object();

		readonly CloudService[] _services;
		readonly ILog _logger;

		bool _isStopRequested;
		bool _isStopped;

		public CloudService[] Services
		{
			get { return _services; }
		}

		public ServiceBalancerCommand(ILog logger, ProvidersForCloudStorage providers)
		{
			_services = null;
			_logger = logger;
			_services = CloudService.GetAllServices(providers).ToArray();
		}

		public void Execute()
		{
			var index = 0;

			// number of allowed runs before going to sleep
			var runCount = _services.Length;

			while(!_isStopRequested)
			{
				// TODO: need to implement here a naive bandit pattern to run services
				var service = _services[index++%_services.Length];
				var isRun = service.Start();

				runCount += isRun ? _services.Length : -1;
				runCount = Math.Max(0, runCount);

				// TODO: we need here a exponential sleeping pattern, gradually
				// increasing the sleep durations

				// when there is nothing to do, it's important not to massively
				// stress all queues for nothing, thus we go to sleep.
				if(runCount == 0)
				{
					// We are not using 'Thread.Sleep' because we want the worker
					// to terminate fast if 'Stop' is requested.
					lock(_sync)
					{
						Monitor.Wait(_sync, IdleSleep.Seconds());
					}
				}
			}

			lock(_sync)
			{
				_isStopped = true;
				Monitor.Pulse(_sync);
			}
		}

		/// <summary>Stops all services.</summary>
		public void Stop()
		{
			_isStopRequested = true;

			lock(_sync)
			{
				while(!_isStopped)
				{
					Monitor.Wait(_sync);
				}
			}
		}
	}
}
