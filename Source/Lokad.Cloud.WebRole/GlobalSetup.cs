#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Framework;
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

			builder.Register(c => (ITypeMapperProvider) new TypeMapperProvider());
			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			Container = builder.Build();
		}

		/// <summary>
		/// Assembly version, cached on startup.
		/// </summary>
		public static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

	}
}