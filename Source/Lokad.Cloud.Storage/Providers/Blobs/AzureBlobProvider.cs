#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Providers.Blobs
{
	public class AzureBlobProvider : IBlobProvider
	{
		private readonly CloudBlobClient _blobClient;

		public AzureBlobProvider(CloudBlobClient blobClient)
		{
			_blobClient = blobClient;
		}

		[Idempotent]
		public Task CreateContainerIfNotExists(string containerName, CancellationToken cancellationToken)
		{
			var container = _blobClient.GetContainerReference(containerName);
			return Task.Factory.FromAsyncRetryWithResult(container.BeginCreateIfNotExist, container.EndCreateIfNotExist, AzurePolicies.TransientServerErrorBackOff, null, cancellationToken);
		}

		[Idempotent]
		public Task DeleteContainerIfExists(string containerName, CancellationToken cancellationToken)
		{
			var container = _blobClient.GetContainerReference(containerName);
			return Task.Factory.FromAsyncRetry(container.BeginDelete, container.EndDelete, AzurePolicies.TransientServerErrorBackOff, null, cancellationToken);
		}
	}
}
