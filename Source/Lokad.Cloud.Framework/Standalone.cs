#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Autofac.Configuration;

namespace Lokad.Cloud
{
	/// <summary>
	/// Provider factory for standalone use of the cloud storage toolkit (O/C mapping)
	/// (if not hosted as worker services in the ServiceFabric).
	/// </summary>
	public static class Standalone
	{
		/// <summary>
		/// Create standalone infrastructure providers using the specified settings.
		/// </summary>
		public static CloudInfrastructureProviders CreateProviders(ICloudConfigurationSettings settings)
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new CloudModule());
			builder.RegisterModule(new CloudConfigurationModule(settings));

			using (var container = builder.Build())
			{
				return container.Resolve<CloudInfrastructureProviders>();
			}
		}

		/// <summary>
		/// Create standalone infrastructure providers using the specified settings.
		/// </summary>
		public static CloudInfrastructureProviders CreateProviders(string dataConnectionString)
		{
			return CreateProviders(new RoleConfigurationSettings
				{
					DataConnectionString = dataConnectionString
				});
		}

		/// <summary>
		/// Create standalone infrastructure providers using an IoC module configuration
		/// in the local config file in the specified config section.
		/// </summary>
		public static CloudInfrastructureProviders CreateProvidersFromConfiguration(string configurationSectionName)
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new CloudModule());
			builder.RegisterModule(new ConfigurationSettingsReader(configurationSectionName));

			using (var container = builder.Build())
			{
				return container.Resolve<CloudInfrastructureProviders>();
			}
		}

		/// <summary>
		/// Create standalone mock infrastructure providers.
		/// </summary>
		/// <returns></returns>
		public static CloudInfrastructureProviders CreateMockProviders()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new CloudModule());
			builder.RegisterModule(new Mock.MockStorageModule());

			using (var container = builder.Build())
			{
				return container.Resolve<CloudInfrastructureProviders>();
			}
		}

		/// <summary>
		/// Create standalone infrastructure providers bound to the local development storage.
		/// </summary>
		public static CloudInfrastructureProviders CreateDevelopmentStorageProviders()
		{
			return CreateProviders("UseDevelopmentStorage=true");
		}
	}
}
