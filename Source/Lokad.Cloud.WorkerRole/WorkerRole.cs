#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Core;
using Lokad.Cloud.Framework;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud
{
	/// <summary>Entry point of Lokad.Cloud.</summary>
	public class WorkerRole : RoleEntryPoint
	{
		public override void Start()
		{
			RoleManager.WriteToLog("Information", "Worker Process entry point called.");

			var builder = new ContainerBuilder();
			builder.RegisterModule(new StorageModule());

			builder.Register(c => (ITypeMapperProvider)new TypeMapperProvider());
			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			builder.Register(typeof(ProvidersForCloudStorage));
			builder.Register(typeof(ServiceBalancerCommand));

			using (var build = builder.Build())
			{
				// balancer endlessly keeps pinging queues for pending work
				var balancer = build.Resolve<ServiceBalancerCommand>();
				balancer.Execute();
			}
		}

		public override RoleStatus GetHealthStatus()
		{
			// This is a sample worker implementation. Replace with your logic.
			return RoleStatus.Healthy;
		}
	}
}
