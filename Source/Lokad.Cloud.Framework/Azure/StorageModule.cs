#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Autofac;
using Autofac.Builder;
using Lokad.Quality;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Module=Autofac.Builder.Module;

namespace Lokad.Cloud.Azure
{
	/// <summary>IoC module that auto-load storage credential along with 
	/// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
	/// <see cref="TableStorageProvider"/> from the IoC settings.</summary>
	[NoCodeCoverage]
	public sealed class StorageModule : Module, ICloudConnectionSettings
	{
		/// <summary>Azure Storage connection string.</summary>
		[UsedImplicitly]
		public string DataConnectionString { get; set; }

		/// <summary>
		/// Provides configuration properties when they are not available from
		/// RoleManager (optional, can be null).
		/// </summary>
		internal RoleConfigurationSettings ExternalRoleConfiguration { get; set; }

		protected override void Load(ContainerBuilder builder)
		{
			if (ExternalRoleConfiguration != null)
			{
				DataConnectionString = ExternalRoleConfiguration.DataConnectionString;
			}
			else
			{
				var config = RoleConfigurationSettings.LoadFromRoleEnvironment();
				if (config.HasValue)
				{
					DataConnectionString = config.Value.DataConnectionString;
				}
			}

			// Only register storage components if the storage credentials are OK
			// This will cause exceptions to be thrown quite soon, but this way
			// the roles' OnStart() method returns correctly, allowing the web role
			// to display a warning to the user (the worker is recycled indefinitely
			// as Run() throws almost immediately)

			if (string.IsNullOrEmpty(DataConnectionString))
			{
				return;
			}

			builder.Register(this).As<ICloudConnectionSettings>();
		}
	}

	/// <summary>
	/// Settings used by the <see cref="StorageModuleWithSettings"></see>
	/// </summary>
	public interface ICloudConnectionSettings
	{
		/// <summary>
		/// Gets the data connection string.
		/// </summary>
		/// <value>The data connection string.</value>
		string DataConnectionString { get; }
	}

	/// <summary>IoC module that registers
	/// <see cref="BlobStorageProvider"/>, <see cref="QueueStorageProvider"/> and
	/// <see cref="TableStorageProvider"/> from the <see cref="ICloudConnectionSettings"/>.</summary>
	public sealed class StorageModuleWithSettings : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			
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
			// ReSharper restore ConvertClosureToMethodGroup
		}

		private static CloudStorageAccount StorageAccountFromSettings(IContext c)
		{
			var settings = c.Resolve<ICloudConnectionSettings>();
			CloudStorageAccount account;
			if (CloudStorageAccount.TryParse(settings.DataConnectionString, out account))
			{
				return account;
			}
			throw new InvalidOperationException("Failed to get valid connection string");
		}

		static ITableStorageProvider TableStorageProvider(IContext c)
		{
			IBinaryFormatter formatter;
			if (!c.TryResolve(out formatter))
			{
				formatter = new CloudFormatter();
			}

			return new TableStorageProvider(c.Resolve<CloudTableClient>(), formatter);
		}

		static IQueueStorageProvider QueueStorageProvider(IContext c)
		{
			IBinaryFormatter formatter;
			if (!c.TryResolve(out formatter))
			{
				formatter = new CloudFormatter();
			}

			return new QueueStorageProvider(
				c.Resolve<CloudQueueClient>(),
				c.Resolve<IBlobStorageProvider>(),
				formatter,
				c.Resolve<IRuntimeFinalizer>());
		}

		static IBlobStorageProvider BlobStorageProvider(IContext c)
		{
			IBinaryFormatter formatter;
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