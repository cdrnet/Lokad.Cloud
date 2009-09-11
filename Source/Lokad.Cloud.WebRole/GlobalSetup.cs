#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	public static class GlobalSetup
	{
		public static readonly IContainer Container;

		static GlobalSetup()
		{
			var builder = new ContainerBuilder();

			// loading configuration from the Azure Service Configuration
			if (RoleManager.IsRoleManagerRunning)
			{
				builder.RegisterModule(new StorageModule());
			}
			else // or from the web.config directly (when azure config is not available)
			{
				builder.RegisterModule(new ConfigurationSettingsReader("autofac"));
			}

			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			Container = builder.Build();
		}

		private static string _storageAccountName = null;
		private static object _syncRoot = new object();

		/// <summary>
		/// Storage account name, cached at startup.
		/// </summary>
		public static string StorageAccountName
		{
			get
			{
				// This synchronization scheme is surely a bit overkill in this case...
				if(null == _storageAccountName)
				{
					lock(_syncRoot)
					{
						if(null == _storageAccountName)
							_storageAccountName = Container.Resolve<Microsoft.Samples.ServiceHosting.StorageClient.BlobStorage>().AccountName;
					}
				}

				return _storageAccountName;
			}
		}

		/// <summary>
		/// Assembly version, cached on startup.
		/// </summary>
		public static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

	}
}