#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using Autofac;
using Autofac.Builder;
using Lokad.Cloud.Management;
using Lokad.Serialization;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Module=Autofac.Builder.Module;

namespace Lokad.Cloud.Storage.Azure
{
	/// <summary>IoC module that registers
	/// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
	/// <see cref="TableStorageProvider"/> from the <see cref="ICloudConfigurationSettings"/>.</summary>
	public sealed class StorageModule : Module
	{
		static StorageModule()
		{
			
		}

		protected override void Load(ContainerBuilder builder)
		{
			builder.Register<CloudFormatter>().As<IDataSerializer>().DefaultOnly();

			// .NET 3.5 compiler can't infer types properly here, hence the directive
			// After moving to VS2010 (in .NET 3.5 mode), lambdas
			// could be replaced with the method groups.

			// ReSharper disable ConvertClosureToMethodGroup
			builder.Register(context => StorageAccountFromSettings(context));
			builder.Register(context => QueueClient(context));
			builder.Register(context => BlobClient(context));
			builder.Register(context => TableClient(context));

			builder.Register(context => BlobStorageProvider(context));
			builder.Register(context => QueueStorageProvider(context));
			builder.Register(context => TableStorageProvider(context));
			builder.Register(context => CloudInfrastructureProviders(context));
			// ReSharper restore ConvertClosureToMethodGroup
		}

		private static CloudStorageAccount StorageAccountFromSettings(IContext c)
		{
			var settings = c.Resolve<ICloudConfigurationSettings>();
			CloudStorageAccount account;
			if (CloudStorageAccount.TryParse(settings.DataConnectionString, out account))
			{
				// http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
				ServicePointManager.FindServicePoint(account.BlobEndpoint).UseNagleAlgorithm = false;
				ServicePointManager.FindServicePoint(account.TableEndpoint).UseNagleAlgorithm = false;
				ServicePointManager.FindServicePoint(account.QueueEndpoint).UseNagleAlgorithm = false;

				return account;
			}
			throw new InvalidOperationException("Failed to get valid connection string");
		}

		static CloudInfrastructureProviders CloudInfrastructureProviders(IContext c)
		{
			return new CloudInfrastructureProviders(
				// storage providers supporting the O/C mapper scenario
				c.Resolve<IBlobStorageProvider>(),
				c.Resolve<IQueueStorageProvider>(),
				c.Resolve<ITableStorageProvider>(),

				// optional providers supporting the execution framework scenario
				c.ResolveOptional<ILog>(),
				c.ResolveOptional<IProvisioningProvider>(),
				c.ResolveOptional<IRuntimeFinalizer>());
		}

		static ITableStorageProvider TableStorageProvider(IContext c)
		{
			IDataSerializer formatter;
			if (!c.TryResolve(out formatter))
			{
				formatter = new CloudFormatter();
			}

			return new TableStorageProvider(c.Resolve<CloudTableClient>(), formatter);
		}

		static IQueueStorageProvider QueueStorageProvider(IContext c)
		{
			IDataSerializer formatter;
			if (!c.TryResolve(out formatter))
			{
				formatter = new CloudFormatter();
			}

			return new QueueStorageProvider(
				c.Resolve<CloudQueueClient>(),
				c.Resolve<IBlobStorageProvider>(),
				formatter,
				// RuntimeFinalizer is a dependency (as the name suggest) on the worker runtime
				// This dependency is typically not available in a pure O/C mapper scenario.
				// In such case, we just pass a dummy finalizer (that won't be used any
				c.ResolveOptional<IRuntimeFinalizer>());
		}

		static IBlobStorageProvider BlobStorageProvider(IContext c)
		{
			IDataSerializer formatter;
			if (!c.TryResolve(out formatter))
			{
				formatter = new CloudFormatter();
			}

			return new BlobStorageProvider(c.Resolve<CloudBlobClient>(), formatter);
		}

		static CloudTableClient TableClient(IContext c)
		{
			var account = c.Resolve<CloudStorageAccount>();
			var storage = account.CreateCloudTableClient();
			storage.RetryPolicy = BuildDefaultRetry();
			return storage;
		}

		static CloudBlobClient BlobClient(IContext c)
		{
			var account = c.Resolve<CloudStorageAccount>();
			var storage = account.CreateCloudBlobClient();
			storage.RetryPolicy = BuildDefaultRetry();
			return storage;
		}

		static CloudQueueClient QueueClient(IContext c)
		{
			var account = c.Resolve<CloudStorageAccount>();
			var queueService = account.CreateCloudQueueClient();
			queueService.RetryPolicy = BuildDefaultRetry();
			return queueService;
		}

		static RetryPolicy BuildDefaultRetry()
		{
			// [abdullin]: in short this gives us MinBackOff + 2^(10)*Rand.(~0.5.Seconds())
			// at the last retry. Reflect the method for more details
			var deltaBackoff = 0.5.Seconds();
			return RetryPolicies.RetryExponential(10, deltaBackoff);
		}
	}
}