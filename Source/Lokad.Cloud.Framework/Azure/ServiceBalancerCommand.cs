#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac;
using Autofac.Builder;

namespace Lokad.Cloud.Azure
{
	/// <summary>Organize the executions of the services.</summary>
	public class ServiceBalancerCommand : ICommand
	{
		/// <summary>Duration to keep pinging the same cloud service if service is active.</summary>
		const int MoreOfTheSame = 60;

		/// <summary>Resting duration expressed in seconds.</summary>
		const int IdleSleep = 5;

		readonly object _sync = new object();

		readonly ProvidersForCloudStorage _providers;
		readonly ILog _logger;

		CloudService[] _services;

		bool _isStopRequested;
		bool _isStopped;

		/// <summary>Container used to populate cloud service properties.</summary>
		public ContainerBuilder ContainerBuilder { get; set; }

		/// <summary>IoC constructor.</summary>
		public ServiceBalancerCommand(ILog logger, ProvidersForCloudStorage providers)
		{
			_providers = providers;
			_logger = logger;
		}

		public void Execute()
		{
			var loader = new AssemblyLoader(_providers.BlobStorage);

			var assemblies = loader.Load();

			// HACK: directly grabing all client modules and instanciating them "by hand"
			var clientModuleTypes = assemblies.Select(a => a.GetExportedTypes()).SelectMany(x => x)
				.Where(t => t.GetInterfaces().Contains(typeof (IModule)) && !t.IsAbstract && !t.IsGenericType);
			
			var clientModules = clientModuleTypes.Select(t => (IModule)t.InvokeMember("_ctor", 
				BindingFlags.CreateInstance, null, null, new object[0]));

			foreach (var clientModule in clientModules)
			{
				ContainerBuilder.RegisterModule(clientModule);
			}

			// HACK: resetting field in order to be able to build a second time
			var field = ContainerBuilder.GetType().
				GetField("_wasBuilt", BindingFlags.Instance | BindingFlags.NonPublic);
			field.SetValue(ContainerBuilder, false);

			var clientContainer = ContainerBuilder.Build();

			_services = CloudService.GetAllServices(clientContainer).ToArray();

			var index = 0;

			// numbers of services that did not execute in a row
			var noRunCount = 0;

			while(!_isStopRequested)
			{
				var service = _services[index++%_services.Length];
				
				var isRunOnce = false;
				var isRun = true;

				// 'more of the same pattern'
				// as long the service is active, keep triggering the same service
				// for at least 1min (in order to avoid a single service to monopolize CPU)
				var start = DateTime.UtcNow;
				while (DateTime.UtcNow.Subtract(start) < MoreOfTheSame.Seconds() && isRun && !_isStopRequested)
				{
					try
					{
						isRun = service.Start();
						isRunOnce |= isRun;
					}
					catch (TriggerRestartException ex)
					{
						// services can cause overall restart
						_logger.Info(ex, string.Format("Restart requested by service {0}.", service));
						throw;
					}
					catch (Exception ex)
					{
						_logger.Error(ex, string.Format("Service {0} has failed.", service));
					}
				}

				noRunCount = isRunOnce ? 0 : noRunCount + 1;

				// TODO: we need here a exponential sleeping pattern (increasing sleep durations)

				// when there is nothing to do, it's important not to massively
				// stress all queues for nothing, thus we go to sleep.
				if(noRunCount >= _services.Length)
				{
					// We are not using 'Thread.Sleep' because we want the worker
					// to terminate fast if 'Stop' is requested.
					lock(_sync)
					{
						Monitor.Wait(_sync, IdleSleep.Seconds());
					}

					noRunCount = 0;
				}

				// throws a 'TriggerRestartException' if a new package is detected.
				loader.CheckUpdate(true);
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
