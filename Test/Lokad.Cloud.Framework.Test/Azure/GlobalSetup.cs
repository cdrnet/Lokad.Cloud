#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud;

namespace Lokad.Cloud.Azure.Test
{
	public sealed class GlobalSetup
	{
		static IContainer _container;

		static IContainer SetUp()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader("autofac"));

			builder.Register(c => (ITypeMapperProvider)new TypeMapperProvider());
			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			builder.Register(typeof (ProvidersForCloudStorage));
			builder.Register(typeof (ServiceBalancerCommand));

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