#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Web
{
	public static class GlobalSetup
	{
		public static readonly IContainer Container;

		static GlobalSetup()
		{
			var builder = new ContainerBuilder();

			// loading configuration from the Azure Service Configuration
			if (RoleEnvironment.IsAvailable)
			{
				builder.RegisterModule(new StorageModule());
				builder.RegisterModule(new AzureManagementModule());
			}
			else // or from the web.config directly (when azure config is not available)
			{
				builder.RegisterModule(new ConfigurationSettingsReader("autofac"));
			}

			// Diagnostics
			builder.RegisterModule(new DiagnosticsModule());

			// Management
			builder.RegisterModule(new ManagementModule());
			builder.Register<LokadCloudVersion>().SingletonScoped();
			builder.Register<LokadCloudUserRoles>().FactoryScoped();

			Container = builder.Build();
		}

		private static readonly object _syncRoot = new object();
		private static string _storageAccountName;

		/// <summary>Storage account name, cached at startup.</summary>
		public static string StorageAccountName
		{
			get
			{
				// This synchronization scheme is surely a bit overkill in this case...
				if (null == _storageAccountName)
				{
					lock (_syncRoot)
					{
						if (null == _storageAccountName)
						{
							_storageAccountName = Container.Resolve<CloudBlobClient>().Credentials.AccountName;
						}
					}
				}

				return _storageAccountName;
			}
		}
	}
}