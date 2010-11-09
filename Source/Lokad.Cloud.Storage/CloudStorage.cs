#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Net;
using Lokad.Serialization;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage
{
    public static class CloudStorage
    {
        public static CloudStorageBuilder ForAzureAccount(CloudStorageAccount storageAccount)
        {
            return new AzureCloudStorageBuilder(storageAccount);
        }

        public static CloudStorageBuilder ForAzureConnectionString(string connectionString)
        {
            CloudStorageAccount storageAccount;
            if (!CloudStorageAccount.TryParse(connectionString, out storageAccount))
            {
                throw new InvalidOperationException("Failed to get valid connection string");
            }

            return new AzureCloudStorageBuilder(storageAccount);
        }

        public static CloudStorageBuilder ForDevelopmentStorage()
        {
            return new AzureCloudStorageBuilder(CloudStorageAccount.DevelopmentStorageAccount);
        }

        public static CloudStorageBuilder ForInMemoryStorage()
        {
            return new InMemoryStorageBuilder();
        }
    }

    public abstract class CloudStorageBuilder
    {
        /// <remarks>Can not be null</remarks>
        protected IDataSerializer DataSerializer { get; private set; }

        /// <remarks>Can be null if not needed</remarks>
        protected IRuntimeFinalizer RuntimeFinalizer { get; private set; }

        protected CloudStorageBuilder()
        {
            // defaults
            DataSerializer = new CloudFormatter();
        }

        public CloudStorageBuilder WithDataSerializer(IDataSerializer dataSerializer)
        {
            DataSerializer = dataSerializer;
            return this;
        }

        public CloudStorageBuilder WithRuntimeFinalizer(IRuntimeFinalizer runtimeFinalizer)
        {
            RuntimeFinalizer = runtimeFinalizer;
            return this;
        }

        public abstract Blobs.IBlobStorageProvider BuildBlobStorage();
        public abstract Tables.ITableStorageProvider BuildTableStorage();
        public abstract Queues.IQueueStorageProvider BuildQueueStorage();

        public CloudStorageProviders BuildStorageProviders()
        {
            return new CloudStorageProviders(
                BuildBlobStorage(),
                BuildQueueStorage(),
                BuildTableStorage(),
                RuntimeFinalizer);
        }
    }

    internal sealed class InMemoryStorageBuilder : CloudStorageBuilder
    {
        public override Blobs.IBlobStorageProvider BuildBlobStorage()
        {
            return new InMemory.MemoryBlobStorageProvider
            {
                DataSerializer = DataSerializer
            };
        }

        public override Tables.ITableStorageProvider BuildTableStorage()
        {
            return new InMemory.MemoryTableStorageProvider
            {
                DataSerializer = DataSerializer
            };
        }

        public override Queues.IQueueStorageProvider BuildQueueStorage()
        {
            return new InMemory.MemoryQueueStorageProvider
            {
                DataSerializer = DataSerializer
            };
        }
    }

    internal sealed class AzureCloudStorageBuilder : CloudStorageBuilder
    {
        private readonly CloudStorageAccount _storageAccount;

        internal AzureCloudStorageBuilder(CloudStorageAccount storageAccount)
        {
            _storageAccount = storageAccount;

            // http://blogs.msdn.com/b/windowsazurestorage/archive/2010/06/25/nagle-s-algorithm-is-not-friendly-towards-small-requests.aspx
            ServicePointManager.FindServicePoint(storageAccount.TableEndpoint).UseNagleAlgorithm = false;
            ServicePointManager.FindServicePoint(storageAccount.QueueEndpoint).UseNagleAlgorithm = false;
        }

        public override Blobs.IBlobStorageProvider BuildBlobStorage()
        {
            return new Azure.BlobStorageProvider(BlobClient(), DataSerializer);
        }

        public override Tables.ITableStorageProvider BuildTableStorage()
        {
            return new Azure.TableStorageProvider(TableClient(), DataSerializer);
        }

        public override Queues.IQueueStorageProvider BuildQueueStorage()
        {
            return new Azure.QueueStorageProvider(
                QueueClient(),
                BuildBlobStorage(),
                DataSerializer,
                RuntimeFinalizer);
        }

        CloudBlobClient BlobClient()
        {
            var blobClient = _storageAccount.CreateCloudBlobClient();
            blobClient.RetryPolicy = BuildDefaultRetry();
            return blobClient;
        }

        CloudTableClient TableClient()
        {
            var tableClient = _storageAccount.CreateCloudTableClient();
            tableClient.RetryPolicy = BuildDefaultRetry();
            return tableClient;
        }

        CloudQueueClient QueueClient()
        {
            var queueClient = _storageAccount.CreateCloudQueueClient();
            queueClient.RetryPolicy = BuildDefaultRetry();
            return queueClient;
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
