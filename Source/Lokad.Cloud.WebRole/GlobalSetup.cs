#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Microsoft.WindowsAzure;

namespace Lokad.Cloud.Web
{
	public static class GlobalSetup
	{
		public static readonly IContainer Container;

		static GlobalSetup()
		{
			var builder = new ContainerBuilder();

			builder.RegisterModule(new CloudModule());

			// loading configuration from the Azure Service Configuration
			var settings = RoleConfigurationSettings.LoadFromRoleEnvironment();
			if (settings.HasValue)
			{
				builder.RegisterModule(new CloudConfigurationModule(settings.Value));
			}
			else
			{
				// or from the web.config directly (when azure config is not available)
				builder.RegisterModule(new ConfigurationSettingsReader("autofac"));
			}

			// Web specific
			builder.Register<LokadCloudVersion>().SingletonScoped();
			builder.Register<LokadCloudUserRoles>().FactoryScoped();

			Container = builder.Build();
		}

		private static readonly object SyncRoot = new object();
		private static string _storageAccountName;

		/// <summary>Storage account name, cached at startup.</summary>
		public static string StorageAccountName
		{
			get
			{
				// This synchronization scheme is surely a bit overkill in this case...
				if (null == _storageAccountName)
				{
					lock (SyncRoot)
					{
						if (null == _storageAccountName)
						{
							_storageAccountName = Container.Resolve<CloudStorageAccount>().Credentials.AccountName;
						}
					}
				}

				return _storageAccountName;
			}
		}
	}
}