#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Core;
using ILog=log4net.ILog;

namespace PingPongClient
{
	class Program
	{
		static void Main(string[] args)
		{
			// TODO: no logic implemented yet
		}

		static Autofac.IContainer Setup()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader("autofac"));

			var policy = ActionPolicy
				.With(HandleException)
				.Retry(10, (e, i) => SystemUtil.Sleep(5.Seconds()));

			builder.Register(policy);

			builder.Register(c => (ITypeMapperProvider)new TypeMapperProvider());
			builder.Register(c => (ILog)new CloudLogger(c.Resolve<IBlobStorageProvider>()));

			return builder.Build();
		}

		static bool HandleException(Exception ex)
		{
			//if (ex is StorageServerException)
			//	return true;

			return false;
		}
	}
}
