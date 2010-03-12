#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Reflection;
using System.Security;
using Autofac.Builder;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>
	/// Performs the work of the <c>WorkerRole</c> in an isolated <see cref="AppDomain"/>.
	/// </summary>
	/// <remarks>
	/// The design of this class is slightly twisted, as it is both caller (in the original AppDomain) 
	/// and callee (in the isolated AppDomain).</remarks>
	internal class IsolatedWorker : MarshalByRefObject
	{
		/// <summary>Refer to the callee instance (isolated). This property is not null
		/// only for the caller instance (non-isolated).</summary>
		IsolatedWorker _isolatedInstance;

		/// <summary>Refer to the isolated runtime. This property is not null
		/// only for the callee instance (isolated).</summary>
		InternalServiceRuntime _runtime;

		/// <summary>Performs the work.</summary>
		/// <returns><c>true</c> if the assemblies were updated and a restart is needed.</returns>
		public bool DoWork()
		{
			// This is necessary to load config values in the main AppDomain because
			// RoleManager is not properly working when invoked from another AppDomain
			// These override values are passed to StorageModule living in another AppDomain
			var roleConfiguration = RoleConfigurationSettings.LoadFromRoleEnvironment();

			// The trick is to load this same assembly in another domain, then
			// instantiate this same class and invoke DoWorkInternal

			var domain = AppDomain.CreateDomain("WorkerDomain", null, AppDomain.CurrentDomain.SetupInformation);

			_isolatedInstance = (IsolatedWorker)domain.CreateInstanceAndUnwrap(
				Assembly.GetExecutingAssembly().FullName, typeof(IsolatedWorker).FullName);

			// This never throws, unless something went wrong with IoC setup and that's fine
			// because it is not possible to execute the worker
			var returnValue = _isolatedInstance.DoWorkInternal(roleConfiguration);

			// If this throws, it's because something went wrong when unloading the AppDomain
			// The exception correctly pulls down the entire worker process so that no AppDomains are
			// left in memory
			AppDomain.Unload(domain);

			return returnValue;
		}

		/// <summary>This method should only be called for the isolated instance.</summary>
		bool DoWorkInternal(Maybe<RoleConfigurationSettings> externalRoleConfiguration)
		{
			var builder = new ContainerBuilder();

			// Cloud Infrastructure
			var storageModule = new StorageModule();
			if (externalRoleConfiguration.HasValue)
			{
				storageModule.ExternalRoleConfiguration = externalRoleConfiguration.Value;
			}
			builder.RegisterModule(storageModule);

			// Diagnostics
			builder.RegisterModule(new DiagnosticsModule());

			// Self Management
			builder.RegisterModule(new ProvisioningModule(externalRoleConfiguration));

			// Runtime
			builder.Register(typeof(RuntimeFinalizer)).As<IRuntimeFinalizer>();
			builder.Register(typeof(InternalServiceRuntime)).FactoryScoped();

			// Providers for cloud apps
			builder.Register(typeof(CloudInfrastructureProviders));

			using (var container = builder.Build())
			{
				var restartForAssemblyUpdate = false;

				var log = container.Resolve<ILog>();
				//log.Log(LogLevel.Info, "Isolated worker started.");

				_runtime = null;
				try
				{
					_runtime = container.Resolve<InternalServiceRuntime>();
					_runtime.RuntimeContainer = container;

					// runtime endlessly keeps pinging queues for pending work
					_runtime.Execute();
				}
				catch (TypeLoadException typeLoadEx)
				{
					log.Log(LogLevel.Error, typeLoadEx, string.Format(
						"Type {0} could not be loaded (service: {1}).",
						typeLoadEx.TypeName,
						GetServiceInExecution(_runtime)));
				}
				catch (FileLoadException fileLoadEx)
				{
					// Tentatively: referenced assembly is missing
					log.Log(LogLevel.Error, fileLoadEx, string.Format(
						"Could not load assembly probably due to a missing reference assembly (service: {0}).",
						GetServiceInExecution(_runtime)));
				}
				catch (SecurityException securityEx)
				{
					// Tentatively: assembly cannot be loaded due to security config
					log.Log(LogLevel.Error, securityEx, string.Format(
						"Could not load assembly {0} probably due to security configuration (service: {1}).",
						securityEx.FailedAssemblyInfo,
						GetServiceInExecution(_runtime)));
				}
				catch (TriggerRestartException)
				{
					//log.Log(LogLevel.Debug, "TriggerRestartException");
					restartForAssemblyUpdate = true;
				}
				catch (Exception ex)
				{
					// Generic exception
					log.Log(LogLevel.Error, ex, string.Format(
						"An unhandled exception occurred (service: {0}).",
						GetServiceInExecution(_runtime)));
				}

				return restartForAssemblyUpdate;
			}
		}

		/// <summary>Called when the environment is being stopped.</summary>
		public void OnStop()
		{
			if (null != _isolatedInstance)
			{
				_isolatedInstance.OnStopInternal();
			}
		}

		/// <summary>This method should only be called for the isolated instance.</summary>
		void OnStopInternal()
		{
			if (null != _runtime)
			{
				_runtime.Stop();
			}
		}

		static string GetServiceInExecution(InternalServiceRuntime runtime)
		{
			string service;
			return runtime == null || String.IsNullOrEmpty(service = runtime.ServiceInExecution)
				? "unknown"
				: service;
		}
	}
}
