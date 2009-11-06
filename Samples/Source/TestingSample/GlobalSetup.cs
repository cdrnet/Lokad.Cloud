
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud;
using Lokad.Cloud.Azure;
using Lokad;
using Lokad.Cloud.Diagnostics;

namespace TestingSample
{
	public sealed class GlobalSetup
	{
		static IContainer _container;

		static IContainer SetUp()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader("autofac"));

			builder.Register(c => new CloudLogger(c.Resolve<IBlobStorageProvider>())).As<ILog>();
			builder.Register(c => new ServiceMonitor(c.Resolve<IBlobStorageProvider>())).As<IServiceMonitor>();

			builder.Register(typeof(CloudInfrastructureProviders));
			builder.Register(typeof(ServiceBalancerCommand));

			return builder.Build();
		}

		/// <summary>Gets the IoC container as initiliazed by the setup.</summary>
		public static IContainer Container
		{
			get
			{
				if(null == _container)
				{
					_container = SetUp();
				}

				return _container;
			}
		}
	}
}
