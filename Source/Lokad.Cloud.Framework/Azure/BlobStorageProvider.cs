﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Lokad.Threading;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Azure
{
	/// <summary>Provides access to the Blob Storage.</summary>
	/// <remarks>
	/// All the methods of <see cref="BlobStorageProvider"/> are thread-safe.
	/// </remarks>
	public class BlobStorageProvider : IBlobStorageProvider
	{
		readonly CloudBlobClient _blobStorage;
		readonly IBinaryFormatter _formatter;

		public BlobStorageProvider(CloudBlobClient blobStorage, IBinaryFormatter formatter)
		{
			_blobStorage = blobStorage;
			_formatter = formatter;
		}

		public bool CreateContainer(string containerName)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			try
			{
				container.Create();
				return true;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ContainerAlreadyExists
					|| ex.ErrorCode == StorageErrorCode.ResourceAlreadyExists) return false;
				throw;
			}
		}

		public bool DeleteContainer(string containerName)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			try
			{
				container.Delete();
				return true;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.ResourceNotFound) return false;
				throw;
			}
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
			return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag);
		}

		public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag)
		{
			using(var stream = new MemoryStream())
			{
				_formatter.Serialize(stream, item);

				etag = null;

				// StorageClient already deals with spliting large items
				var container = _blobStorage.GetContainerReference(containerName);

				try
				{
					var blob = container.GetBlockBlobReference(blobName);
					try
					{
						blob.FetchAttributes();
					}
					catch { }

					if(blob.Properties.ETag == null || (blob.Properties.ETag != null && overwrite))
					{
						stream.Seek(0, SeekOrigin.Begin);
						blob.UploadFromStream(stream);
						blob.FetchAttributes();
						etag = blob.Properties.ETag;
						return true;
					}

					return false;
				}
				catch(StorageClientException ex)
				{
					// if the container does not exist, it gets created
					if(ex.ErrorCode == StorageErrorCode.ContainerNotFound)
					{
						// caution the container might have been freshly deleted
						var flag = false;
						string tempEtag = null;
						PolicyHelper.SlowInstantiation.Do(() =>
						{
							container.CreateIfNotExist();

							var myBlob = container.GetBlockBlobReference(blobName);
							try
							{
								myBlob.FetchAttributes();
							}
							catch { }

							if(myBlob.Properties.ETag == null || (myBlob.Properties.ETag != null && overwrite))
							{
								stream.Seek(0, SeekOrigin.Begin);
								myBlob.UploadFromStream(stream);
								myBlob.FetchAttributes();
								tempEtag = myBlob.Properties.ETag;
								flag = true;
							}
						});

						if(flag) etag = tempEtag;

						return flag;
					}
					if(ex.ErrorCode == StorageErrorCode.BlobAlreadyExists && !overwrite)
					{
						// See http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/fff78a35-3242-4186-8aee-90d27fbfbfd4
						// and http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/86b9f184-c329-4c30-928f-2991f31e904b/

						return false;
					}

					var blob = container.GetBlockBlobReference(blobName);
					try
					{
						blob.FetchAttributes();
					}
					catch { }

					if(blob.Properties.ETag == null || (blob.Properties.ETag != null && overwrite))
					{
						stream.Seek(0, SeekOrigin.Begin);
						blob.UploadFromStream(stream);
						blob.FetchAttributes();
						etag = blob.Properties.ETag;
						return true;
					}

					return false;
				}
			}
		}

		public T GetBlob<T>(string containerName, string blobName)
		{
			string ignoredEtag;
			return GetBlob<T>(containerName, blobName, out ignoredEtag);
		}

		public T GetBlob<T>(string containerName, string blobName, out string etag)
		{
			var output = GetBlob(containerName, blobName, typeof(T), out etag);
			if(output == null) return default(T);
			else return (T)output;
		}

		public object GetBlob(string containerName, string blobName, Type type, out string etag)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			var blob = container.GetBlockBlobReference(blobName);

			using(var stream = new MemoryStream())
			{
				etag = null;

				// no such container, return default
				try
				{
					blob.FetchAttributes();
					blob.DownloadToStream(stream);

					etag = blob.Properties.ETag;
				}
				catch(StorageClientException ex)
				{
					if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
						|| ex.ErrorCode == StorageErrorCode.BlobNotFound
						|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
					{
						return null;
					}
					throw;
				}

				stream.Seek(0, SeekOrigin.Begin);
				return _formatter.Deserialize(stream, type);
			}
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
			var container = _blobStorage.GetContainerReference(containerName);
			newEtag = null;

			CloudBlockBlob blob = null;

			try
			{
				blob = container.GetBlockBlobReference(blobName);
				blob.FetchAttributes();
				if(blob.Properties == null || blob.Properties.ETag == oldEtag)
				{
					return default(T);
				}
				newEtag = blob.Properties.ETag;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound
					|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					return default(T);
				}
				throw;
			}

			using(var stream = new MemoryStream())
			{
				blob.DownloadToStream(stream);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)_formatter.Deserialize(stream, typeof(T));
			}
		}

		public string GetBlobEtag(string containerName, string blobName)
		{
			var container = _blobStorage.GetContainerReference(containerName);

			try
			{
				var blob = container.GetBlockBlobReference(blobName);
				blob.FetchAttributes();
				return blob.Properties.ETag;
			}
			catch (StorageClientException ex)
			{
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound
					|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					return null;
				}
				throw;
			}
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater)
		{
			return UpdateIfNotModified<T>(containerName, blobName, x => Result.CreateSuccess(updater(x)));
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, Result<T>> updater)
		{
			Result<T> ignored;
			return UpdateIfNotModified(containerName, blobName, updater, out ignored);
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater, out T result)
		{
			Result<T> rresult;
			var flag = UpdateIfNotModified(containerName, blobName, x => Result.CreateSuccess(updater(x)), out rresult);

			result = rresult.Value;
			return flag;
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, Result<T>> updater, out Result<T> result)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			CloudBlockBlob blob = null;

			T input;
			try
			{
				blob = container.GetBlockBlobReference(blobName);
				using(var rstream = new MemoryStream())
				{
					blob.DownloadToStream(rstream);
					rstream.Seek(0, SeekOrigin.Begin);
					input = (T)_formatter.Deserialize(rstream, typeof(T));
				}
			}
			catch (StorageClientException ex)
			{
				// creating the container when missing
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound 
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound
					|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					input = default(T);
					container.CreateIfNotExist();

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

			using(var wstream = new MemoryStream())
			{
				_formatter.Serialize(wstream, result.Value);
				wstream.Seek(0, SeekOrigin.Begin);
				blob.UploadFromStream(wstream);
			}

			return true;
		}

		public bool DeleteBlob(string containerName, string blobName)
		{
			var container = _blobStorage.GetContainerReference(containerName);

			try
			{
				var blob = container.GetBlockBlobReference(blobName);
				blob.Delete();
				return true;
			}
			catch (StorageClientException ex) // no such container, return false
			{
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound
					|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					return false;
				}
				throw;
			}
		}

		public IEnumerable<string> List(string containerName, string prefix)
		{
			// HACK: quick and dirty implementation that replaces the old one below
			// http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/c5e36676-8d07-46cc-b803-72621a0898b0/?prof=required

			if(prefix == null) prefix = "";

			var container = _blobStorage.GetContainerReference(containerName);

			BlobRequestOptions options = new BlobRequestOptions();
			options.UseFlatBlobListing = true;

			var enumerator = container.ListBlobs(options).GetEnumerator();

			while(true)
			{
				try
				{
					if(!enumerator.MoveNext())
					{
						yield break;
					}
				}
				catch(StorageClientException ex)
				{
					// if the container does not exist, empty enumeration
					if(ex.ErrorCode == StorageErrorCode.ContainerNotFound)
					{
						yield break;
					}
					throw;
				}

				var stringUri = enumerator.Current.Uri.ToString();

				// URI is like this
				// http://azure.whatever.com/container/the-name -- or
				// http://azure.whatever.com/container/a-prefix/the-name -- or
				// http://azure.whatever.com/container/a-prefix/another-prefix/the-name -- etc

				// Full name is extracted like this:
				// Find the first two slashes and take everything after them
				// Find the first slash and take everything after it
				// Find the first slash and take everything after it (again)

				stringUri = stringUri.Substring(stringUri.IndexOf("//") + 2);
				var name = stringUri.Substring(stringUri.IndexOf("/") + 1);
				name = name.Substring(name.IndexOf("/") + 1);

				if(name.StartsWith(prefix)) yield return name;
			}
		}

		[Obsolete]
		private IEnumerable<string> ListOld(string containerName, string prefix)
		{
			throw new NotImplementedException();

			/*var container = _blobStorage.GetContainerReference(containerName);

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
			}*/
		}
	}
}
