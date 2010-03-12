#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

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
	public sealed class StorageModule : Module
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

			CloudStorageAccount account;
			if (!CloudStorageAccount.TryParse(DataConnectionString, out account))
			{
				return;
			}

			builder.Register(c =>
				{
					var queueService = account.CreateCloudQueueClient();
					queueService.RetryPolicy = BuildDefaultRetry();
					return queueService;
				});

			builder.Register(c =>
				{
					var storage = account.CreateCloudBlobClient();
					storage.RetryPolicy = BuildDefaultRetry();
					return storage;
				});

			builder.Register(c =>
				{
					var storage = account.CreateCloudTableClient();
					storage.RetryPolicy = BuildDefaultRetry();
					return storage;
				});

			// registering the Lokad.Cloud providers
			builder.Register(c =>
				{
					IBinaryFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new CloudFormatter();
					}

					return (IBlobStorageProvider) new BlobStorageProvider(c.Resolve<CloudBlobClient>(), formatter);
				});

			builder.Register(c =>
				{
					IBinaryFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new CloudFormatter();
					}

					return (IQueueStorageProvider) new QueueStorageProvider(
						c.Resolve<CloudQueueClient>(),
						c.Resolve<IBlobStorageProvider>(),
						formatter,
						c.Resolve<IRuntimeFinalizer>());
				});

			builder.Register(c =>
				{
					IBinaryFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new CloudFormatter();
					}

					return (ITableStorageProvider) new TableStorageProvider(c.Resolve<CloudTableClient>(), formatter);
				});
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