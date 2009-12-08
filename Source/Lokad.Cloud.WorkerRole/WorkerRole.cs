#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using Autofac.Builder;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
	/// <summary>Entry point of Lokad.Cloud.</summary>
	public class WorkerRole : RoleEntryPoint
	{
		bool _isHealthy = true;

		/// <summary>
		/// Called by Windows Azure to initialize the role instance.
		/// </summary>
		/// <returns>
		/// True if initialization succeeds, False if it fails. The default implementation returns True.
		/// </returns>
		/// <remarks>
		/// <para>Any exception that occurs within the OnStart method is an unhandled exception.</para>
		/// </remarks>
		public override bool OnStart()
		{
			RoleEnvironment.Changing += (sender, args) => RoleEnvironment.RequestRecycle();

			return base.OnStart();
		}

		/// <summary>
		/// Called by Windows Azure after the role instance has been initialized. This
		/// method serves as the main thread of execution for your role.
		/// </summary>
		/// <remarks>
		/// <para>The role recycles when the Run method returns.</para>
		/// <para>Any exception that occurs within the Run method is an unhandled exception.</para>
		/// </remarks>
		public override void Run()
		{
			var restartPolicy = new NoRestartFloodPolicy(isHealthy => { _isHealthy = isHealthy; });
			restartPolicy.Do(() =>
			{
				var worker = new IsolatedWorker();
				return worker.DoWork();
			});
		}
	}

	/// <summary>
	/// Performs the work of the <see cref="WorkerRole"/> in an isolated <see cref="AppDomain"/>.
	/// </summary>
	public class IsolatedWorker : MarshalByRefObject
	{
		/// <summary>Performs the work.</summary>
		/// <returns><c>true</c> if the assemblies were updated and a restart is needed.</returns>
		public bool DoWork()
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
			var returnValue = isolatedInstance.DoWorkInternal(overrides);

			// If this throws, it's because something went wrong when unloading the AppDomain
			// The exception correctly pulls down the entire worker process so that no AppDomains are
			// left in memory
			AppDomain.Unload(domain);

			return returnValue;
		}

		public bool DoWorkInternal(Dictionary<string, string> overrides)
		{
			var builder = new ContainerBuilder();

			// Cloud Infrastructure
			var storageModule = new StorageModule {OverriddenProperties = overrides};
			builder.RegisterModule(storageModule);
			builder.Register(typeof(CloudInfrastructureProviders));

			// Diagnostics
			builder.RegisterModule(new DiagnosticsModule());

			// Services
			builder.Register(typeof(ServiceBalancerCommand));

			using (var container = builder.Build())
			{
				bool restartForAssemblyUpdate = false;

				var log = container.Resolve<ILog>();
				log.Log(LogLevel.Info, "Isolated worker started.");

				ServiceBalancerCommand balancer = null;
				try
				{
					balancer = container.Resolve<ServiceBalancerCommand>();
					balancer.ContainerBuilder = builder;

					// balancer endlessly keeps pinging queues for pending work
					balancer.Execute();
				}
				catch(TypeLoadException typeLoadEx)
				{
					log.Log(LogLevel.Error, typeLoadEx, string.Format(
						"Type {0} could not be loaded (service: {1}).",
						typeLoadEx.TypeName,
						GetServiceInExecution(balancer)));
				}
				catch(FileLoadException fileLoadEx)
				{
					// Tentatively: referenced assembly is missing
					log.Log(LogLevel.Error, fileLoadEx, string.Format(
						"Could not load assembly probably due to a missing reference assembly (service: {0}).",
						GetServiceInExecution(balancer)));
				}
				catch(SecurityException securityEx)
				{
					// Tentatively: assembly cannot be loaded due to security config
					log.Log(LogLevel.Error, securityEx, string.Format(
						"Could not load assembly {0} probably due to security configuration (service: {1}).",
						securityEx.FailedAssemblyInfo,
						GetServiceInExecution(balancer)));
				}
				catch(TriggerRestartException)
				{
					//log.Log(LogLevel.Debug, "TriggerRestartException");
					restartForAssemblyUpdate = true;
				}
				catch(Exception ex)
				{
					// Generic exception
					log.Log(LogLevel.Error, ex, string.Format(
						"An unhandled exception occurred (service: {0}).",
						GetServiceInExecution(balancer)));
				}
				
				return restartForAssemblyUpdate;
			}
		}

		static string GetServiceInExecution(ServiceBalancerCommand balancer)
		{
			if (balancer == null || String.IsNullOrEmpty(balancer.ServiceInExecution))
			{
				return "unknown";
			}
			
			return balancer.ServiceInExecution;
		}
	}

}
