#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reflection;
using Autofac.Builder;
using Lokad.Cloud;
using Lokad.Cloud.Azure;
using Microsoft.ServiceHosting.ServiceRuntime;
using System.Collections.Generic;

namespace Lokad.Cloud
{
	/// <summary>Entry point of Lokad.Cloud.</summary>
	public class WorkerRole : RoleEntryPoint
	{
		public override void Start()
		{
			RoleManager.WriteToLog("Information", "Worker Process entry point called.");

			while(true)
			{
				IsolatedWorker worker = new IsolatedWorker();
				worker.DoWork();
			}
		}

		public override RoleStatus GetHealthStatus()
		{
			return RoleStatus.Healthy;
		}
	}

	/// <summary>
	/// Performs the work of the WorkerRole in an isolated AppDomain.
	/// </summary>
	public class IsolatedWorker : MarshalByRefObject
	{

		/// <summary>
		/// Performs the work.
		/// </summary>
		public void DoWork()
		{
			Dictionary<string, string> overrides = new Dictionary<string, string>();
			overrides.Add("AccountName", RoleManager.GetConfigurationSetting("AccountName"));
			overrides.Add("AccountKey", RoleManager.GetConfigurationSetting("AccountKey"));
			overrides.Add("BlobEndpoint", RoleManager.GetConfigurationSetting("BlobEndpoint"));
			overrides.Add("QueueEndpoint", RoleManager.GetConfigurationSetting("QueueEndpoint"));

			//IsolatedWorker isolatedInstance = new IsolatedWorker();
			//isolatedInstance.DoWorkInternal(overrides);

			// This trick is to load this same assembly in another domain, then
			// instantiate this same class and invoke DoWorkInternal

			/*AppDomainSetup setup = new AppDomainSetup();
			setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
			setup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;*/
			AppDomain domain = AppDomain.CreateDomain("WorkerDomain", null, AppDomain.CurrentDomain.SetupInformation);
			/*foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				domain.Load(asm.FullName);
			}*/

			IsolatedWorker isolatedInstance = (IsolatedWorker)domain.CreateInstanceAndUnwrap(
				Assembly.GetExecutingAssembly().FullName, typeof(IsolatedWorker).FullName);

			// This never throws, unless something went wrong with IoC setup and that's fine
			// because it is not possible to execute the worker
			isolatedInstance.DoWorkInternal(overrides);

			// If this throws, it's because something went wrong when unloading the AppDomain
			// The exception correctly pulls down the entire worker process so that no AppDomains are
			// left in memory
			AppDomain.Unload(domain);
		}

		public void DoWorkInternal(Dictionary<string, string> overrides)
		{
			var builder = new ContainerBuilder();
			var storageModule = new StorageModule();
			storageModule.OverriddenProperties = overrides;
			builder.RegisterModule(storageModule);

			builder.Register(c => (ITypeMapperProvider)new TypeMapperProvider());
			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			builder.Register(typeof(ProvidersForCloudStorage));
			builder.Register(typeof(ServiceBalancerCommand));

			using (var container = builder.Build())
			{
				var log = container.Resolve<ILog>();
				log.Log(LogLevel.Info, "Isolated worker started.");

				try
				{	
					var balancer = container.Resolve<ServiceBalancerCommand>();
					balancer.Container = container;

					// balancer endlessly keeps pinging queues for pending work
					balancer.Execute();
				}
				catch (TriggerRestartException)
				{
					var logger = container.Resolve<ILog>();
					logger.Log(LogLevel.Info, "Requesting worker restart...");

					return;
				}
				catch(Exception ex)
				{
					var logger = container.Resolve<ILog>();
					logger.Log(LogLevel.Error, ex, "Executor level exception (probably a Lokad.Cloud issue).");

					return;
				}
			}
		}

	}

}
