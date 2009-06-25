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
			PutBlob(containerName, blobName, item, true);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite)
		{
			var stream = new MemoryStream();
			_formatter.Serialize(stream, item);
			var buffer = stream.GetBuffer();

			// StorageClient already deals with spliting large items
			var container = _blobStorage.GetBlobContainer(containerName);
			try
			{
				return container.CreateBlob(new BlobProperties(blobName), new BlobContents(buffer), overwrite);
			}
			catch (StorageClientException ex)
			{
				// if the container does not exist, it gets created
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
				{
					container.CreateContainer();
				}

				return container.CreateBlob(new BlobProperties(blobName), new BlobContents(buffer), overwrite);
			}
		}

		public T GetBlob<T>(string containerName, string blobName)
		{
			var blobContents = new BlobContents(new MemoryStream());
			var container = _blobStorage.GetBlobContainer(containerName);
			
			// no such container, return default
			try
			{
				var properties = container.GetBlob(blobName, blobContents, false);

				if (null == properties) return default(T);
			}
			catch(StorageClientException ex)
			{
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound)
				{
					return default(T);
				}
				throw;
			}

			var stream = blobContents.AsStream;
			stream.Position = 0;
			return (T)_formatter.Deserialize(stream);
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater, out T result)
		{
			var blobContents = new BlobContents(new MemoryStream());
			var container = _blobStorage.GetBlobContainer(containerName);
			BlobProperties properties = null;

			try
			{
				properties = container.GetBlob(blobName, blobContents, false);

				if (null == properties)
				{
					result = default(T);
				}
				else
				{
					var rstream = blobContents.AsStream;
					rstream.Position = 0;
					result = (T)_formatter.Deserialize(rstream);
				}
			}
			catch (StorageClientException ex)
			{
				// creating the container when missing
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
				{
					result = default(T);
					container.CreateContainer();
				}
				else
				{
					throw;
				}
			}
			
			// updating the item
			result = updater(result);

			var wstream = new MemoryStream();
			_formatter.Serialize(wstream, result);
			var buffer = wstream.GetBuffer();

			blobContents = new BlobContents(buffer);

			return null == properties ? 
				container.CreateBlob(properties, blobContents, false) : 
				container.UpdateBlobIfNotModified(properties, blobContents);
		}

		public bool DeleteBlob(string containerName, string blobName)
		{
			var container = _blobStorage.GetBlobContainer(containerName);

			try
			{
				return container.DeleteBlob(blobName);
			}
			catch (StorageClientException ex) // no such container, return false
			{
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
				{
					return false;
				}
				throw;
			}
		}

		public IEnumerable<string> List(string containerName, string prefix)
		{
			var container = _blobStorage.GetBlobContainer(containerName);

			// Trick: 'ListBlobs' is lazilly enumerating over the blob storage
			// only the minimal amount of network call will be made depending on
			// the number of items actually enumerated.

			var enumerator = container.ListBlobs(prefix, false).GetEnumerator();

			while(true)
			{
				try
				{
					if(!enumerator.MoveNext())
					{
						yield break;
					}
				}
				catch (StorageClientException ex)
				{
					// if the container does not exist, empty enumeration
					if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
					{
						yield break;
					}
					throw;
				}

				// 'yield return' cannot appear in try/catch block
				yield return ((BlobProperties)enumerator.Current).Name;
			}
		}
	}
}
