#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Web.Security;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Core;
using Microsoft.Samples.ServiceHosting.StorageClient;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	internal static class GlobalSetup
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

			var policy = ActionPolicy
				.With(HandleException)
				.Retry(10, (e, i) => SystemUtil.Sleep(5.Seconds()));

			builder.Register(policy);

			builder.Register(c => (ITypeMapperProvider) new TypeMapperProvider());
			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			Container = builder.Build();
		}

		static bool HandleException(Exception ex)
		{
			if (ex is StorageServerException)
			{
				return true;
			}

			return false;
		}
	}
}