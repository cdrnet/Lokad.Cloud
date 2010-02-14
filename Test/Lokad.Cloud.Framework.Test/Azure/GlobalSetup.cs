#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Azure.Test
{
	public sealed class GlobalSetup
	{
		static IContainer _container;

		static IContainer SetUp()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader("autofac"));

			// formatter
			builder.Register(typeof (CloudFormatter)).As<IBinaryFormatter>();

			// Cloud Infrastructure
			builder.Register(typeof(CloudInfrastructureProviders));

			// Diagnostics
			builder.RegisterModule(new DiagnosticsModule());

			// Self Management
			builder.RegisterModule(new AzureManagementModule());

			return builder.Build();
		}

		/// <summary>Gets the IoC container as initialized by the setup.</summary>
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