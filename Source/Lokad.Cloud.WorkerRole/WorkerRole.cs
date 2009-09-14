#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Autofac.Builder;
using Lokad.Cloud.Azure;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud
{
	/// <summary>Entry point of Lokad.Cloud.</summary>
	public class WorkerRole : RoleEntryPoint
	{
		const int MaxConsecutiveFailures = 3;

		int _consecutiveFailures;

		public override void Start()
		{
			RoleManager.WriteToLog("Information", "Worker Process entry point called.");

			Interlocked.Exchange(ref _consecutiveFailures, 0);
			var completedPrevious = true;

			while(true)
			{
				var worker = new IsolatedWorker();
				var completed = worker.DoWork(_consecutiveFailures >= MaxConsecutiveFailures);

				if(!completed) Interlocked.Increment(ref _consecutiveFailures);
				else Interlocked.Exchange(ref _consecutiveFailures, 0);

				completedPrevious = completed;
			}
		}

		public override RoleStatus GetHealthStatus()
		{
			if(_consecutiveFailures >= MaxConsecutiveFailures) return RoleStatus.Unhealthy;
			else return RoleStatus.Healthy;
		}
	}

	/// <summary>
	/// Performs the work of the WorkerRole in an isolated AppDomain.
	/// </summary>
	public class IsolatedWorker : MarshalByRefObject
	{
		/// <summary>Performs the work. </summary>
		/// <param name="onlyWithNewAssemblies">Specifies whether the actual work should be started only if assemblies are new.</param>
		/// <returns><c>true</c> if the worker completed successfully, <c>false</c> if it crashed.</returns>
		public bool DoWork(bool onlyWithNewAssemblies)
		{
			// This is necessary to load config values in the main AppDomain because
			// RoleManager is not properly working when invoked from another AppDomain
			// These override values are passed to StorageModule living in another AppDomain
			var overrides = StorageModule.GetPropertiesValuesFromRuntime();

			// The trick is to load this same assembly in another domain, then
			// instantiate this same class and invoke DoWorkInternal

			var domain = AppDomain.CreateDomain("WorkerDomain", null, AppDomain.CurrentDomain.SetupInformation);

			var isolatedInstance = (IsolatedWorker)domain.CreateInstanceAndUnwrap(
				Assembly.GetExecutingAssembly().FullName, typeof(IsolatedWorker).FullName);

			// This never throws, unless something went wrong with IoC setup and that's fine
			// because it is not possible to execute the worker
			var completed = isolatedInstance.DoWorkInternal(overrides, onlyWithNewAssemblies);

			// If this throws, it's because something went wrong when unloading the AppDomain
			// The exception correctly pulls down the entire worker process so that no AppDomains are
			// left in memory
			AppDomain.Unload(domain);

			return completed;
		}

		public bool DoWorkInternal(Dictionary<string, string> overrides, bool onlyWithNewAssemblies)
		{
			var builder = new ContainerBuilder();
			var storageModule = new StorageModule {OverriddenProperties = overrides};
			builder.RegisterModule(storageModule);

			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			builder.Register(typeof(ProvidersForCloudStorage));
			builder.Register(typeof(ServiceBalancerCommand));

			using (var container = builder.Build())
			{
				var loader = new AssemblyLoader(container.Resolve<IBlobStorageProvider>());
				loader.LoadPackage();

				var log = container.Resolve<ILog>();
				log.Log(LogLevel.Info, "Isolated worker started.");

				// Halt execution until new assemblies are loaded
				if(onlyWithNewAssemblies)
				{
					log.Log(LogLevel.Warn, "Isolated worker has crashed several times and is now waiting for new assemblies.");

					while(true)
					{
						try
						{
							loader.CheckUpdate(true);
						}
						catch(TriggerRestartException)
						{
							// Continue with normal execution
							// It is necessary to restart the AppDomain in order to reload assemblies
							return true;
						}
						Thread.Sleep(1000);
					}
				}

				try
				{
					var balancer = container.Resolve<ServiceBalancerCommand>();
					balancer.AssemblyLoader = loader;
					balancer.ContainerBuilder = builder;

					// balancer endlessly keeps pinging queues for pending work
					balancer.Execute();

					return true;
				}
				catch(TriggerRestartException)
				{
					return true;
				}
				catch(CloudServiceException)
				{
					return false;
				}
				catch(Exception ex)
				{
					var logger = container.Resolve<ILog>();
					logger.Log(LogLevel.Error, ex, "Executor level exception (probably a Lokad.Cloud issue).");

					return false;
				}
			}
		}

	}

}
