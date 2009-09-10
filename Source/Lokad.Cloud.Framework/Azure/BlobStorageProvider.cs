#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cloud;
using Lokad.Threading;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Azure
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
			string ignored = null;
			return PutBlob<T>(containerName, blobName, item, overwrite, out ignored);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag)
		{
			var stream = new MemoryStream();
			_formatter.Serialize(stream, item);
			var buffer = stream.GetBuffer();

			etag = null;

			// StorageClient already deals with spliting large items
			var container = _blobStorage.GetBlobContainer(containerName);
			BlobProperties blobProperties = new BlobProperties(blobName);

			try
			{
				bool created = container.CreateBlob(blobProperties, new BlobContents(buffer), overwrite);

				if(created) etag = blobProperties.ETag;

				return created;
			}
			catch(StorageClientException ex)
			{
				// if the container does not exist, it gets created
				if(ex.ErrorCode == StorageErrorCode.ContainerNotFound)
				{
					// caution the container might have been freshly deleted
					var flag = false;
					PolicyHelper.SlowInstantiation.Do(() =>
					{
						container.CreateContainer();

						flag = container.CreateBlob(
							blobProperties,
							new BlobContents(buffer), overwrite);
					});

					if(flag) etag = blobProperties.ETag;

					return flag;
				}
				if(ex.ErrorCode == StorageErrorCode.BlobAlreadyExists && !overwrite)
				{
					// See http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/fff78a35-3242-4186-8aee-90d27fbfbfd4
					// and http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/86b9f184-c329-4c30-928f-2991f31e904b/

					return false;
				}

				bool created = container.CreateBlob(blobProperties, new BlobContents(buffer), overwrite);

				if(created) etag = blobProperties.ETag;

				return created;
			}
		}

		public T GetBlob<T>(string containerName, string blobName)
		{
			string ignoredEtag;
			return GetBlob<T>(containerName, blobName, out ignoredEtag);
		}

		public T GetBlob<T>(string containerName, string blobName, out string etag)
		{
			var blobContents = new BlobContents(new MemoryStream());
			var container = _blobStorage.GetBlobContainer(containerName);
			etag = null;

			// no such container, return default
			try
			{
				var properties = container.GetBlob(blobName, blobContents, false);
				if (null == properties) return default(T);
				etag = properties.ETag;
			}
			catch (StorageClientException ex)
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

		public T[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags)
		{	
			var tempResult = blobNames.SelectInParallel(blobName =>
				{
					string etag = null;
					T blob = GetBlob<T>(containerName, blobName, out etag);
					return new Tuple<T, string>(blob, etag);
				}, blobNames.Length);

			etags = new string[blobNames.Length];
			var result = new T[blobNames.Length];

			for(int i = 0; i < tempResult.Length; i++) {
				result[i] = tempResult[i].Item1;
				etags[i] = tempResult[i].Item2;
			}

			return result;
		}

		public T GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag)
		{
			var blobContents = new BlobContents(new MemoryStream());
			var container = _blobStorage.GetBlobContainer(containerName);
			newEtag = null;

			// no such container, return default
			try
			{
				BlobProperties props = new BlobProperties(blobName);
				props.ETag = oldEtag;
				bool available = container.GetBlobIfModified(props, blobContents, true);
				if(!available) return default(T);
				newEtag = props.ETag;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
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

		public string GetBlobEtag(string containerName, string blobName)
		{
			var container = _blobStorage.GetBlobContainer(containerName);

			try
			{
				var properties = container.GetBlobProperties(blobName);
				return null == properties ? null : properties.ETag;
			}
			catch (StorageClientException ex)
			{
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
				{
					return null;
				}
				throw;
			}
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater)
		{
			return UpdateIfNotModified<T>(containerName, blobName, x => Result.Success(updater(x)));
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, Result<T>> updater)
		{
			Result<T> ignored;
			return UpdateIfNotModified(containerName, blobName, updater, out ignored);
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater, out T result)
		{
			Result<T> rresult;
			var flag = UpdateIfNotModified(containerName, blobName, x => Result.Success(updater(x)), out rresult);

			result = rresult.Value;
			return flag;
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, Result<T>> updater, out Result<T> result)
		{
			var blobContents = new BlobContents(new MemoryStream());
			var container = _blobStorage.GetBlobContainer(containerName);
			BlobProperties properties = null;

			T input;
			try
			{
				properties = container.GetBlob(blobName, blobContents, false);

				var rstream = blobContents.AsStream;
				rstream.Position = 0;
				input = (T)_formatter.Deserialize(rstream);
			}
			catch (StorageClientException ex)
			{
				// creating the container when missing
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound 
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound)
				{
					input = default(T);
					container.CreateContainer();

					// HACK: if the container has just been deleted,
					// creation could be delayed and would need a proper handling.
				}
				else
				{
					throw;
				}
			}
			
			// updating the item
			result = updater(input);

			if(!result.IsSuccess)
			{
				return false;
			}

			var wstream = new MemoryStream();
			_formatter.Serialize(wstream, result.Value);
			var buffer = wstream.GetBuffer();

			blobContents = new BlobContents(buffer);

			return null == properties ? 
				container.CreateBlob(new BlobProperties(blobName), blobContents, false) : 
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
