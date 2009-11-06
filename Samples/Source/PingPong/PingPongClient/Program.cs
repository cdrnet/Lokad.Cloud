#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Lokad.Cloud;
using Lokad.Cloud.Diagnostics;
using ILog=Lokad.ILog;

namespace PingPongClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var container = Setup();

			var provider = container.Resolve<IQueueStorageProvider>();

			var input = new[] {0.0, 1.0, 2.0};

			// pushing item to the 'ping' queue
			provider.PutRange("ping", input);
			foreach(var x in input)
			{
				Console.Write("ping={0} ", x);
			}
			Console.WriteLine();

			Console.WriteLine("Queued 3 items in 'ping'.");
			// items are going to be processed by the service

			// getting items from the 'pong' queue
			for(int i = 0; i < 100; i++) 
			{
				foreach(var x in provider.Get<double>("pong", 10))
				{
					Console.Write("pong={0} ", x);
					provider.Delete("pong", x);
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

			builder.Register(c => new CloudLogger(c.Resolve<IBlobStorageProvider>())).As<ILog>();
			builder.Register(c => new ServiceMonitor(c.Resolve<IBlobStorageProvider>())).As<IServiceMonitor>();

			return builder.Build();
		}
	}
}
