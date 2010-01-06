#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Azure
{
	/// <summary>Organize the executions of the services.</summary>
	public class ServiceBalancerCommand : ICommand
	{
		/// <summary>Duration (seconds) to keep pinging the same cloud service if service is active.</summary>
		const int MoreOfTheSame = 60;

		/// <summary>Resting duration (seconds).</summary>
		const int IdleSleep = 5;

		readonly object _sync = new object();

		readonly CloudInfrastructureProviders _providers;
		readonly IServiceMonitor _monitoring;
		readonly DiagnosticsAcquisition _diagnostics;

		CloudService[] _services;

		bool _isStopRequested;
		bool _isStopped;

		/// <summary>Container used to populate cloud service properties.</summary>
		public ContainerBuilder ContainerBuilder { get; set; }

		/// <summary>The name of the service that is being executed, if any, <c>null</c> otherwise.</summary>
		public string ServiceInExecution { get; private set; }

		/// <summary>IoC constructor.</summary>
		public ServiceBalancerCommand(
			CloudInfrastructureProviders providers,
			ICloudDiagnosticsRepository diagnosticsRepository)
		{
			_providers = providers;
			_monitoring = new ServiceMonitor(diagnosticsRepository);
			_diagnostics = new DiagnosticsAcquisition(diagnosticsRepository);
		}

		public void Execute()
		{
			var loader = new AssemblyLoader(_providers.BlobStorage);

			loader.LoadPackage();
			var config = loader.LoadConfiguration();

			// processing configuration file as retrieved from the blob storage.
			if (config.HasValue)
			{
				const string fileName = "lokad.cloud.clientapp.config";
				string pathToFile;

				// HACK: hard-code string for local storage name
				if (RoleEnvironment.IsAvailable)
				{
					var localResource = RoleEnvironment.GetLocalResource("LokadCloudStorage");
					pathToFile = Path.Combine(localResource.RootPath, fileName);
				}
				else
				{
					pathToFile = Path.Combine(Path.GetTempPath(), fileName);
				}

				using (var stream = File.Open(pathToFile, FileMode.Create, FileAccess.ReadWrite))
				{
					// writing config locally
					stream.Write(config.Value, 0, config.Value.Length);
				}

				// HACK: need to copy settings locally first
				var configReader = new ConfigurationSettingsReader("autofac", pathToFile);

				ContainerBuilder.RegisterModule(configReader);
			}

			// HACK: resetting field in order to be able to build a second time
			var field = ContainerBuilder.GetType().
				GetField("_wasBuilt", BindingFlags.Instance | BindingFlags.NonPublic);
			field.SetValue(ContainerBuilder, false);

			var clientContainer = ContainerBuilder.Build();

			_services = CloudService.GetAllServices(clientContainer).ToArray();

			// give the client a chance to register external diagnostics sources
			clientContainer.InjectUnsetProperties(_diagnostics);

			var index = 0;

			// numbers of services that did not execute in a row
			var noRunCount = 0;

			while(!_isStopRequested)
			{
				var service = _services[index++%_services.Length];
				
				var isRunOnce = false;
				var isRun = true;

				try
				{
					using (_monitoring.Monitor(service))
					{
						// 'more of the same pattern'
						// as long the service is active, keep triggering the same service
						// for at least 1min (in order to avoid a single service to monopolize CPU)
						var start = DateTimeOffset.Now;
						while (DateTimeOffset.Now.Subtract(start) < MoreOfTheSame.Seconds() && isRun && !_isStopRequested)
						{
							// No exceptions caught here
							ServiceInExecution = service.Name;
							isRun = service.Start();
							isRunOnce |= isRun;
						}
					}
				}
				catch
				{
					// try to dump diagnostics, but suppress error if it fails
					// (likely because of the same issue the service itself already failed, which is logged)
					try
					{
						_diagnostics.CollectStatistics();
					}
					catch {}

					throw;
				}

				ServiceInExecution = null;

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
