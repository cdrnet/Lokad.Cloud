#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Autofac;
using Autofac.Builder;
using Lokad.Cloud.Core;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Web
{
	internal static class GlobalSetup
	{
		public static readonly IContainer Container;

		static GlobalSetup()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new StorageModule());

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