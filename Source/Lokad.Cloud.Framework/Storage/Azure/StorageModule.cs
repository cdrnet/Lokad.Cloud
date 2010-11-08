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
using Module=Autofac.Builder.Module;

namespace Lokad.Cloud.Storage.Azure
{
	/// <summary>IoC module that registers
	/// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
	/// <see cref="TableStorageProvider"/> from the <see cref="ICloudConfigurationSettings"/>.</summary>
	public sealed class StorageModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register<CloudFormatter>().As<IDataSerializer>().DefaultOnly();

			// ReSharper disable ConvertClosureToMethodGroup
			builder.Register(context => StorageAccountFromSettings(context));

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
			return CloudStorage.ForAzureAccount(c.Resolve<CloudStorageAccount>())
				.WithDataSerializer(c.ResolveOptional<IDataSerializer>() ?? new CloudFormatter())
				.BuildTableStorage();
		}

		static IQueueStorageProvider QueueStorageProvider(IContext c)
		{
			return CloudStorage.ForAzureAccount(c.Resolve<CloudStorageAccount>())
				.WithDataSerializer(c.ResolveOptional<IDataSerializer>() ?? new CloudFormatter())
				.WithRuntimeFinalizer(c.ResolveOptional<IRuntimeFinalizer>())
				.BuildQueueStorage();
		}

		static IBlobStorageProvider BlobStorageProvider(IContext c)
		{
			return CloudStorage.ForAzureAccount(c.Resolve<CloudStorageAccount>())
				.WithDataSerializer(c.ResolveOptional<IDataSerializer>() ?? new CloudFormatter())
				.BuildBlobStorage();
		}
	}
}