#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Diagnostics;
using Lokad.Diagnostics;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>Organize the executions of the services.</summary>
	internal class InternalServiceRuntime
	{
		readonly CloudInfrastructureProviders _providers;
		readonly IServiceMonitor _monitoring;
		readonly DiagnosticsAcquisition _diagnostics;

		/// <summary>Main thread used to schedule services in <see cref="Execute()"/>.</summary>
		Thread _executeThread;

		bool _isStopRequested;
		Scheduler _scheduler;

		/// <summary>Container used to populate cloud service properties.</summary>
		public IContainer RuntimeContainer { get; set; }

		// Instrumentation
		readonly ExceptionCounters _exceptions;

		/// <summary>IoC constructor.</summary>
		public InternalServiceRuntime(
			CloudInfrastructureProviders providers,
			ICloudDiagnosticsRepository diagnosticsRepository)
		{
			_providers = providers;
			_monitoring = new ServiceMonitor(diagnosticsRepository);
			_diagnostics = new DiagnosticsAcquisition(diagnosticsRepository);

			// Instrumentation
			_exceptions = ExceptionCounters.Default;
		}

		/// <summary>The name of the service that is being executed, if any, <c>null</c> otherwise.</summary>
		public string ServiceInExecution
		{
			get
			{
				CloudService service;
				return _scheduler == null || (service = _scheduler.CurrentlyScheduledService) == null
					? null
					: service.Name;
			}
		}

		/// <summary>Called once by the service fabric. Call is not supposed to return
		/// until stop is requested, or an uncaught exception is thrown.</summary>
		public void Execute()
		{
			// hook on the current thread to force shut down
			_executeThread = Thread.CurrentThread;

			var clientContainer = RuntimeContainer;

			var loader = new AssemblyLoader(_providers.BlobStorage);
			loader.LoadPackage();

			// processing configuration file as retrieved from the blob storage.
			var config = loader.LoadConfiguration();
			if (config.HasValue)
			{
				ApplyConfiguration(config.Value, clientContainer);
			}

			// give the client a chance to register external diagnostics sources
			clientContainer.InjectProperties(_diagnostics);

			_scheduler = new Scheduler(() => LoadServices(clientContainer), RunService);
			foreach (var action in _scheduler.Schedule())
			{
				if(_isStopRequested)
				{
					break;
				}

				try
				{
					action();

					// throws a 'TriggerRestartException' if a new package is detected.
					loader.CheckUpdate(true);
				}
				catch(Exception ex)
				{
					if(!(ex is TriggerRestartException))
					{
						_exceptions.Add(ex);
					}

					TryDumpDiagnostics();
					throw;
				}
			}
		}

		/// <summary>Stops all services at once.</summary>
		/// <remarks>Called once by the service fabric when environment is about to
		/// be shut down.</remarks>
		public void Stop()
		{
			_isStopRequested = true;

			if(_executeThread != null)
			{
				_executeThread.Abort();
			}

			// TODO: #128 need for object finalization
		}

		/// <summary>Raise the "stop requested" flag and waits until the service
		/// completes. This method is too slow to be used if the envirronment is
		/// requesting immediate shutdown, but it has the advantage of gracefully
		/// shutting down the services.</summary>
		public void WaitForStop()
		{
			_isStopRequested = true;

			if (_scheduler != null)
			{
				_scheduler.AbortWaitingSchedule();
			}
		}

		/// <summary>
		/// Load and get all service instances using the provided IoC container.
		/// </summary>
		static IEnumerable<CloudService> LoadServices(IContainer container)
		{
			var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetExportedTypes()).SelectMany(x => x)
				.Where(t => t.IsSubclassOf(typeof (CloudService)) && !t.IsAbstract && !t.IsGenericType)
				.ToList();

			var builder = new ContainerBuilder();
			foreach (var type in serviceTypes)
			{
				builder.Register(type)
					.OnActivating(ActivatingHandler.InjectUnsetProperties)
					.FactoryScoped()
					.ExternallyOwned();

				// ExternallyOwned: to prevent the container from disposing the
				// cloud services - we manage their lifetime on our own using
				// e.g. RuntimeFinalizer
			}
			builder.Build(container);

			return serviceTypes.Select(type => (CloudService) container.Resolve(type));
		}

		/// <summary>
		/// Run a scheduled service
		/// </summary>
		ServiceExecutionFeedback RunService(CloudService service)
		{
			ServiceExecutionFeedback feedback;

			using (_monitoring.Monitor(service))
			{
				feedback = service.Start();
			}

			return feedback;
		}

		/// <summary>
		/// Try to dump diagnostics, but suppress any exceptions if it fails
		/// (logging would likely fail as well)
		/// </summary>
		void TryDumpDiagnostics()
		{
			try
			{
				_diagnostics.CollectStatistics();
			}
// ReSharper disable EmptyGeneralCatchClause
			catch { }
// ReSharper restore EmptyGeneralCatchClause
		}

		/// <summary>
		/// Apply the configuration provided in text as raw bytes to the provided IoC
		/// container.
		/// </summary>
		static void ApplyConfiguration(byte[] config, IContainer container)
		{
			// HACK: need to copy settings locally first
			// HACK: hard-code string for local storage name
			const string fileName = "lokad.cloud.clientapp.config";
			const string resourceName = "LokadCloudStorage";

			var pathToFile = Path.Combine(
				CloudEnvironment.GetLocalStoragePath(resourceName),
				fileName);

			File.WriteAllBytes(pathToFile, config);
			var configReader = new ConfigurationSettingsReader("autofac", pathToFile);

			configReader.Configure(container);
		}
	}
}