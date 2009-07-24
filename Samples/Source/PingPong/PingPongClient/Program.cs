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
using Microsoft.Samples.ServiceHosting.StorageClient;
using ILog=log4net.ILog;

namespace PingPongClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var container = Setup();

			var provider = container.Resolve<IQueueStorageProvider>();

			provider.Put("ping", new [] { 0.0, 1.0, 2.0 });

			for(int i = 0; i < 10; i++)
			{
				foreach(var x in provider.Get<double>("ping", 10))
				{
					Console.Write("deq={0} ", x);
				}

				Console.Write("sleep 1000ms. ");
				System.Threading.Thread.Sleep(1000);

				Console.WriteLine();
			}
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
			if (ex is StorageServerException || ex is StorageClientException)
				return true;

			return false;
		}
	}
}
