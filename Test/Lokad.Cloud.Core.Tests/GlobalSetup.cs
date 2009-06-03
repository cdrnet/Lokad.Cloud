#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Autofac.Builder;
using Autofac.Configuration;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NUnit.Framework;

namespace Lokad.Cloud.Core.Tests
{
	[SetUpFixture]
	public sealed class GlobalSetup
	{
		[SetUp]
		public void SetUp()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader("autofac"));

			var policy = ActionPolicy
				.With(HandleException)
				.RetryForever(e => SystemUtil.Sleep(5.Seconds()));

			builder.Register(policy);

			var build = builder.Build();

			var storage = build.Resolve<QueueStorage>();

			Console.WriteLine(storage);
		}

		static bool HandleException(Exception ex)
		{
			if (ex is StorageServerException)
				return true;

			return false;
		}
	}
}
