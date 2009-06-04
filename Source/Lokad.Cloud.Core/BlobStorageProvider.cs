#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Core
{
	/// <summary>Provides access to the Blob Storage.</summary>
	/// <remarks>
	/// All the methods of <see cref="BlobStorageProvider"/> are thread-safe.
	/// </remarks>
	public class BlobStorageProvider : IBlobStorageProvider
	{
		readonly BlobStorage _blobStorage;
		readonly IFormatter _formatter;

		public BlobStorageProvider(BlobStorage blobStorage, IFormatter formatter)
		{
			_blobStorage = blobStorage;
			_formatter = formatter;
		}

		public bool CreateContainer(string containerName)
		{
			var container = _blobStorage.GetBlobContainer(containerName);
			return container.CreateContainer();
		}

		public bool DeleteContainer(string containerName)
		{
			var container = _blobStorage.GetBlobContainer(containerName);
			return container.DeleteContainer();
		}

		public void PutBlob<T>(string containerName, string blobName, T item)
		{
			var stream = new MemoryStream();
			_formatter.Serialize(stream, item);
			var buffer = stream.GetBuffer();

			// StorageClient already deals with spliting large items
			var container = _blobStorage.GetBlobContainer(containerName);
			container.CreateBlob(new BlobProperties(blobName), new BlobContents(buffer), true);
		}

		public T GetBlob<T>(string containerName, string blobName)
		{
			var blobContents = new BlobContents(new MemoryStream());
			var container = _blobStorage.GetBlobContainer(containerName);
			container.GetBlob(blobName, blobContents, false);

			var stream = blobContents.AsStream;
			stream.Position = 0;
			return (T)_formatter.Deserialize(stream);
		}

		public bool DeleteBlob(string containerName, string blobName)
		{
			var container = _blobStorage.GetBlobContainer(containerName);
			return container.DeleteBlob(blobName);
		}

		public IEnumerable<string> List(string containerName, string prefix)
		{
			var container = _blobStorage.GetBlobContainer(containerName);

			foreach(BlobProperties blobProperty in container.ListBlobs(prefix, false))
			{
				yield return blobProperty.Name;
			}
		}
	}
}
