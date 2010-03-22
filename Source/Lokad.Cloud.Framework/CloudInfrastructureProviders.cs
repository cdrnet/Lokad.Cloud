#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Management;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud
{
	/// <summary>IoC argument for <see cref="CloudService"/> and other
	/// cloud abstractions.</summary>
	/// <remarks>This argument will be populated through Inversion Of Control (IoC)
	/// by the Lokad.Cloud framework itself. This class is placed in the
	/// <c>Lokad.Cloud.Framework</c> for convenience while inheriting a
	/// <see cref="CloudService"/>.</remarks>
	public class CloudInfrastructureProviders
	{
		/// <summary>Abstracts the Blob Storage.</summary>
		public IBlobStorageProvider BlobStorage { get; private set; }

		/// <summary>Abstracts the Queue Storage.</summary>
		public IQueueStorageProvider QueueStorage { get; private set; }

		/// <summary>Abstracts the Table Storage.</summary>
		public ITableStorageProvider TableStorage { get; private set; }

		/// <summary>Error Logger</summary>
		public ILog Log { get; private set; }

		/// <summary>Abstracts the Management API.</summary>
		public IProvisioningProvider Provisioning { get; set; }

		/// <summary>Abstracts the finalizer (used for fast resource release
		/// in case of runtime shutdown).</summary>
		public IRuntimeFinalizer RuntimeFinalizer { get; set; }

		/// <summary>IoC constructor.</summary>
		public CloudInfrastructureProviders(
			IBlobStorageProvider blobStorage, 
			IQueueStorageProvider queueStorage,
			ITableStorageProvider tableStorage,
			ILog log,
			IProvisioningProvider provisioning,
			IRuntimeFinalizer runtimeFinalizer)
		{
			BlobStorage = blobStorage;
			QueueStorage = queueStorage;
			TableStorage = tableStorage;
			Log = log;
			Provisioning = provisioning;
			RuntimeFinalizer = runtimeFinalizer;
		}
	}
}
