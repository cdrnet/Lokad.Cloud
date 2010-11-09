﻿#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage
{
    /// <summary>IoC argument for <see cref="CloudService"/> and other
    /// cloud abstractions.</summary>
    /// <remarks>This argument will be populated through Inversion Of Control (IoC)
    /// by the Lokad.Cloud framework itself. This class is placed in the
    /// <c>Lokad.Cloud.Framework</c> for convenience while inheriting a
    /// <see cref="CloudService"/>.</remarks>
    public class CloudStorageProviders
    {
        /// <summary>Abstracts the Blob Storage.</summary>
        public Blobs.IBlobStorageProvider BlobStorage { get; private set; }

        /// <summary>Abstracts the Queue Storage.</summary>
        public Queues.IQueueStorageProvider QueueStorage { get; private set; }

        /// <summary>Abstracts the Table Storage.</summary>
        public Tables.ITableStorageProvider TableStorage { get; private set; }

        /// <summary>Abstracts the finalizer (used for fast resource release
        /// in case of runtime shutdown).</summary>
        public IRuntimeFinalizer RuntimeFinalizer { get; set; }

        /// <summary>IoC constructor.</summary>
        public CloudStorageProviders(
            Blobs.IBlobStorageProvider blobStorage,
            Queues.IQueueStorageProvider queueStorage,
            Tables.ITableStorageProvider tableStorage,
            IRuntimeFinalizer runtimeFinalizer)
        {
            BlobStorage = blobStorage;
            QueueStorage = queueStorage;
            TableStorage = tableStorage;
            RuntimeFinalizer = runtimeFinalizer;
        }
    }
}