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

		readonly ProvidersForCloudStorage _providers;
		readonly ILog _logger;
		readonly AssemblyLoader _loader;

		CloudService[] _services;

		bool _isStopRequested;
		bool _isStopped;

		/// <summary>Get services actually loaded.</summary>
		public CloudService[] Services
		{
			get { return _services; }
		}

		/// <summary>IoC constructor.</summary>
		public ServiceBalancerCommand(ILog logger, ProvidersForCloudStorage providers)
		{
			_providers = providers;
			_logger = logger;
			_loader = new AssemblyLoader(providers.BlobStorage);
		}

		public void Execute()
		{
			_loader.Load();
			_services = CloudService.GetAllServices(_providers).ToArray();

			var index = 0;

			// number of allowed runs before going to sleep
			var runCount = _services.Length;

			while(!_isStopRequested)
			{
				// TODO: need to implement here a naive bandit pattern to run services
				var service = _services[index++%_services.Length];
				
				var isRun = false;
				
				try
				{
					isRun = service.Start();
				}
				catch(TriggerRestartException ex)
				{
					// services can cause overall restart
					_logger.Info(ex, string.Format("Restart requested by service {0}.", service));
					throw;
				}
				catch(Exception ex)
				{
					_logger.Error(ex, string.Format("Service {0} has failed.", service));
				}

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

				// throws a 'TriggerRestartException' if a new package is detected.
				_loader.CheckUpdate(true);
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
