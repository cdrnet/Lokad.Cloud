#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using Lokad.Cloud.Storage.Blobs;
using Lokad.Cloud.Storage.Serialization;
using Lokad.Diagnostics;
using Lokad.Serialization;
using Lokad.Threading;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Lokad.Cloud.Storage.Azure
{
	/// <summary>Provides access to the Blob Storage.</summary>
	/// <remarks>
	/// All the methods of <see cref="BlobStorageProvider"/> are thread-safe.
	/// </remarks>
	public class BlobStorageProvider : IBlobStorageProvider
	{
		private const string MetadataMD5Key = "ContentMD5";

		readonly CloudBlobClient _blobStorage;
		readonly IDataSerializer _serializer;
		readonly ActionPolicy _azureServerPolicy;

		// Instrumentation
		readonly ExecutionCounter _countPutBlob;
		readonly ExecutionCounter _countGetBlob;
		readonly ExecutionCounter _countGetBlobIfModified;
		readonly ExecutionCounter _countUpdateIfNotModified;
		readonly ExecutionCounter _countDeleteBlob;

		public BlobStorageProvider(CloudBlobClient blobStorage, IDataSerializer serializer)
		{
			_blobStorage = blobStorage;
			_serializer = serializer;
			_azureServerPolicy = AzurePolicies.TransientServerErrorBackOff;

			// Instrumentation
			ExecutionCounters.Default.RegisterRange(new[]
				{
					_countPutBlob = new ExecutionCounter("BlobStorageProvider.PutBlob", 0, 0),
					_countGetBlob = new ExecutionCounter("BlobStorageProvider.GetBlob", 0, 0),
					_countGetBlobIfModified = new ExecutionCounter("BlobStorageProvider.GetBlobIfModified", 0, 0),
					_countUpdateIfNotModified = new ExecutionCounter("BlobStorageProvider.UpdateIfNotModified", 0, 0),
					_countDeleteBlob = new ExecutionCounter("BlobStorageProvider.DeleteBlob", 0, 0),
				});
		}

		public bool CreateContainer(string containerName)
		{
			//workaround since Azure is presently returning OutOfRange exception when using a wrong name.
			if (!BlobStorageExtensions.IsContainerNameValid(containerName))
				throw new NotSupportedException("containerName is not compliant with azure constraints on container naming");

			var container = _blobStorage.GetContainerReference(containerName);
			try
			{
				_azureServerPolicy.Do(container.Create);
				return true;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ContainerAlreadyExists
					|| ex.ErrorCode == StorageErrorCode.ResourceAlreadyExists)
				{
					return false;
				}

				throw;
			}
		}

		public bool DeleteContainer(string containerName)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			try
			{
				_azureServerPolicy.Do(container.Delete);
				return true;
			}
			catch(StorageClientException ex)
			{
				if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					return false;
				}

				throw;
			}
		}

		public void PutBlob<T>(string containerName, string blobName, T item)
		{
			PutBlob(containerName, blobName, item, true);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite)
		{
			string ignored;
			return PutBlob(containerName, blobName, item, overwrite, out ignored);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag)
		{
			return PutBlob(containerName, blobName, item, typeof(T), overwrite, out etag);
		}

		public bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag)
		{
			string outEtag;
			return PutBlob(containerName, blobName, item, typeof (T), true, expectedEtag, out outEtag);
		}

		public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, string expectedEtag, out string outEtag)
		{
			var timestamp = _countPutBlob.Open();

			using (var stream = new MemoryStream())
			{
				_serializer.Serialize(item, stream);

				var container = _blobStorage.GetContainerReference(containerName);

				Func<Maybe<string>> doUpload = () =>
				{
					var blob = container.GetBlobReference(blobName);

					// single remote call
					var result = UploadBlobContent(blob, stream, overwrite, expectedEtag);

					return result;
				};

				try
				{
					var result = doUpload();
					if (!result.HasValue)
					{
						outEtag = null;
						return false;
					}

					outEtag = result.Value;
					_countPutBlob.Close(timestamp);
					return true;
				}
				catch (StorageClientException ex)
				{
					// if the container does not exist, it gets created
					if (ex.ErrorCode == StorageErrorCode.ContainerNotFound)
					{
						// caution: the container might have been freshly deleted
						// (multiple retries are needed in such a situation)
						var tentativeEtag = Maybe<string>.Empty;
						AzurePolicies.SlowInstantiation.Do(() =>
						{
							_azureServerPolicy.Get<bool>(container.CreateIfNotExist);

							tentativeEtag = doUpload();
						});

						if (!tentativeEtag.HasValue)
						{
							outEtag = null;
							return false;
						}

						outEtag = tentativeEtag.Value;
						_countPutBlob.Close(timestamp);
						return true;
					}

					if (ex.ErrorCode == StorageErrorCode.BlobAlreadyExists && !overwrite)
					{
						// See http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/fff78a35-3242-4186-8aee-90d27fbfbfd4
						// and http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/86b9f184-c329-4c30-928f-2991f31e904b/

						outEtag = null;
						return false;
					}

					var result = doUpload();
					if (!result.HasValue)
					{
						outEtag = null;
						return false;
					}

					outEtag = result.Value;
					_countPutBlob.Close(timestamp);
					return true;
				}
			}
		}

		public bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string outEtag)
		{
			return PutBlob(containerName, blobName, item, type, overwrite, null, out outEtag);
		}

		Maybe<string> UploadBlobContent(CloudBlob blob, Stream stream, bool overwrite)
		{
			return UploadBlobContent(blob, stream, overwrite, null);
		}

		/// <param name="overwrite">If <c>false</c>, then no write happens if the blob already exists.</param>
		/// <param name="expectedEtag">When specified, no writing occurs unless the blob etag
		/// matches the one specified as argument.</param>
		/// <returns></returns>
		Maybe<string> UploadBlobContent(CloudBlob blob, Stream stream, bool overwrite, string expectedEtag)
		{
			BlobRequestOptions options;

			if (!overwrite) // no overwrite authorized, blob must NOT exists
			{
				options = new BlobRequestOptions { AccessCondition = AccessCondition.IfNotModifiedSince(DateTime.MinValue) };
			}
			else // overwrite is OK
			{
				options = string.IsNullOrEmpty(expectedEtag) ?
																// case with no etag constraint
					new BlobRequestOptions { AccessCondition = AccessCondition.None } :
																						// case with etag constraint
					new BlobRequestOptions { AccessCondition = AccessCondition.IfMatch(expectedEtag) };
			}

			ApplyContentHash(blob, stream);

			try
			{
				_azureServerPolicy.Do(() =>
					{
						stream.Seek(0, SeekOrigin.Begin);
						blob.UploadFromStream(stream, options);
					});
			}
			catch (StorageClientException ex)
			{
				if (ex.ErrorCode == StorageErrorCode.ConditionFailed)
				{
					return Maybe<string>.Empty;
				}

				throw;
			}

			return Maybe.From(blob.Properties.ETag);
		}

		public Maybe<T> GetBlob<T>(string containerName, string blobName)
		{
			string ignoredEtag;
			return GetBlob<T>(containerName, blobName, out ignoredEtag);
		}

		public Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag)
		{
			return GetBlob(containerName, blobName, typeof (T), out etag)
				.Convert<Maybe<T>>(o => (T) o, Maybe<T>.Empty);
		}

		public Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag)
		{
			var timestamp = _countGetBlob.Open();

			var container = _blobStorage.GetContainerReference(containerName);
			var blob = container.GetBlockBlobReference(blobName);
			 
			using(var stream = new MemoryStream())
			{
				etag = null;

				// if no such container, return empty
				try
				{
					_azureServerPolicy.Do(() =>
						{
							stream.Seek(0, SeekOrigin.Begin);
							blob.DownloadToStream(stream);
							VerifyContentHash(blob, stream);
						});
					etag = blob.Properties.ETag;
				}
				catch(StorageClientException ex)
				{
					if(ex.ErrorCode == StorageErrorCode.ContainerNotFound
						|| ex.ErrorCode == StorageErrorCode.BlobNotFound
							|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
					{
						return Maybe<object>.Empty;
					}

					throw;
				}

				stream.Seek(0, SeekOrigin.Begin);
				var deserialized = _serializer.Deserialize(stream, type);

				if (deserialized == null)
				{
					return Maybe<object>.Empty;
				}

				_countGetBlob.Close(timestamp);
				return Maybe.From(deserialized);
			}
		}

		public Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag)
		{
			etag = null;

			var formatter = _serializer as IIntermediateDataSerializer;
			if (formatter == null)
			{
				return Maybe<XElement>.Empty;
			}

			var container = _blobStorage.GetContainerReference(containerName);
			var blob = container.GetBlockBlobReference(blobName);

			using (var stream = new MemoryStream())
			{
				// if no such container, return empty
				try
				{
					_azureServerPolicy.Do(() =>
						{
							stream.Seek(0, SeekOrigin.Begin);
							blob.DownloadToStream(stream);
							VerifyContentHash(blob, stream);
						});
					etag = blob.Properties.ETag;
				}
				catch (StorageClientException ex)
				{
					if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
						|| ex.ErrorCode == StorageErrorCode.BlobNotFound
							|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
					{
						return Maybe<XElement>.Empty;
					}

					throw;
				}

				stream.Seek(0, SeekOrigin.Begin);
				var deserialized = formatter.UnpackXml(stream);

				if (deserialized == null)
				{
					return Maybe<XElement>.Empty;
				}

				return Maybe.From(deserialized);
			}
		}

		public Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags)
		{
			var tempResult = blobNames.SelectInParallel(blobName =>
				{
					string etag;
					var blob = GetBlob<T>(containerName, blobName, out etag);
					return new Tuple<Maybe<T>, string>(blob, etag);
				}, blobNames.Length);

			etags = new string[blobNames.Length];
			var result = new Maybe<T>[blobNames.Length];

			for (int i = 0; i < tempResult.Length; i++)
			{
				result[i] = tempResult[i].Item1;
				etags[i] = tempResult[i].Item2;
			}

			return result;
		}

		public Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag)
		{
			// 'oldEtag' is null, then behavior always match simple 'GetBlob'.
			if(null == oldEtag)
			{
				return GetBlob<T>(containerName, blobName, out newEtag);
			}

			var timestamp = _countGetBlobIfModified.Open();

			newEtag = null;

			var container = _blobStorage.GetContainerReference(containerName);
			var blob = container.GetBlobReference(blobName);

			try
			{
				var options = new BlobRequestOptions 
					{ 
						AccessCondition = AccessCondition.IfNoneMatch(oldEtag)
					};

				using (var stream = new MemoryStream())
				{
					_azureServerPolicy.Do(() =>
						{
							stream.Seek(0, SeekOrigin.Begin);
							blob.DownloadToStream(stream, options);
						});

					newEtag = blob.Properties.ETag;

					stream.Seek(0, SeekOrigin.Begin);
					var deserialized = (T)_serializer.Deserialize(stream, typeof(T));
					_countGetBlobIfModified.Close(timestamp);
					return deserialized;
				}
			}
			catch (StorageClientException ex)
			{
				// call fails because blob has been modified (usual case)
				if(ex.ErrorCode == StorageErrorCode.ConditionFailed ||
					// HACK: BUG in StorageClient 1.0 
					// see http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/4817cafa-12d8-4979-b6a7-7bda053e6b21
					ex.Message == @"The condition specified using HTTP conditional header(s) is not met.")
				{
					return Maybe<T>.Empty;
				}

				// call fails due to misc problems
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound
						|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					return Maybe<T>.Empty;
				}

				throw;
			}
		}

		public string GetBlobEtag(string containerName, string blobName)
		{
			var container = _blobStorage.GetContainerReference(containerName);

			try
			{
				var blob = container.GetBlockBlobReference(blobName);
				_azureServerPolicy.Do(blob.FetchAttributes);
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

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, T> updater)
		{
			return UpdateIfNotModified<T>(containerName, blobName, x => Result.CreateSuccess(updater(x)));
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, Result<T>> updater)
		{
			Result<T> ignored;
			return UpdateIfNotModified(containerName, blobName, updater, out ignored);
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, T> updater, out T result)
		{
			Result<T> rresult;
			var flag = UpdateIfNotModified(containerName, blobName, x => Result.CreateSuccess(updater(x)), out rresult);

			result = rresult.Value;
			return flag;
		}

		public bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			var timestamp = _countUpdateIfNotModified.Open();

			var container = _blobStorage.GetContainerReference(containerName);
			CloudBlockBlob blob = null;

			Maybe<T> input;
			string originalEtag = null;
			try
			{
				blob = container.GetBlockBlobReference(blobName);

				using (var rstream = new MemoryStream())
				{
					_azureServerPolicy.Do(() =>
						{
							rstream.Seek(0, SeekOrigin.Begin);
							blob.DownloadToStream(rstream);
							VerifyContentHash(blob, rstream);
						});

					originalEtag = blob.Properties.ETag;

					rstream.Seek(0, SeekOrigin.Begin);
					var blobData = _serializer.Deserialize(rstream, typeof(T));

					input = blobData == null ? Maybe<T>.Empty : (T) blobData;
				}
			}
			catch (StorageClientException ex)
			{
				// creating the container when missing
				if (ex.ErrorCode == StorageErrorCode.ContainerNotFound
					|| ex.ErrorCode == StorageErrorCode.BlobNotFound
						|| ex.ErrorCode == StorageErrorCode.ResourceNotFound)
				{
					input = Maybe<T>.Empty;

					// caution: the container might have been freshly deleted
					// (multiple retries are needed in such a situation)
					AzurePolicies.SlowInstantiation.Do(() =>
						_azureServerPolicy.Get<bool>(container.CreateIfNotExist));
				}
				else
				{
					throw;
				}
			}

			// updating the item
			result = updater(input);

			if (!result.IsSuccess)
			{
				return false;
			}

			using (var wstream = new MemoryStream())
			{
				_serializer.Serialize(result.Value, wstream);
				wstream.Seek(0, SeekOrigin.Begin);

				var success = string.IsNullOrEmpty(originalEtag) ? 
																	// no etag, then we should not overwrite a blob created meantime
					UploadBlobContent(blob, wstream, false, null).HasValue : 
																			// existing etag, then we should not overwrite a different etag
					UploadBlobContent(blob, wstream, true, originalEtag).HasValue;

				if(success)
				{
					_countUpdateIfNotModified.Close(timestamp);
				}
				return success;
			}
		}

		public bool DeleteBlob(string containerName, string blobName)
		{
			var timestamp = _countDeleteBlob.Open();

			var container = _blobStorage.GetContainerReference(containerName);

			try
			{
				var blob = container.GetBlockBlobReference(blobName);
				_azureServerPolicy.Do(blob.Delete);
				_countDeleteBlob.Close(timestamp);
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
			// Enumerated blobs do not have a "name" property,
			// thus the name must be extracted from their URI
			// http://social.msdn.microsoft.com/Forums/en-US/windowsazure/thread/c5e36676-8d07-46cc-b803-72621a0898b0/?prof=required

			if(prefix == null) prefix = "";

			var container = _blobStorage.GetContainerReference(containerName);

			var options = new BlobRequestOptions
				{
					UseFlatBlobListing = true
				};

			// if no prefix is provided, then enumerate the whole container
			IEnumerator<IListBlobItem> enumerator;
			if (string.IsNullOrEmpty(prefix))
			{
				enumerator = container.ListBlobs(options).GetEnumerator();
			}
			else
			{
				// 'CloudBlobDirectory' must be used for prefixed enumeration
				var directory = container.GetDirectoryReference(prefix);

				// HACK: [vermorel] very ugly override, but otherwise an "/" separator gets forcibly added
				typeof (CloudBlobDirectory).GetField("prefix", BindingFlags.Instance | BindingFlags.NonPublic)
					.SetValue(directory, prefix);

				enumerator = directory.ListBlobs(options).GetEnumerator();
			}

			while(true)
			{
				try
				{
					if(!_azureServerPolicy.Get<bool>(enumerator.MoveNext))
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

				// removing /container/ from the blob name (dev storage: /account/container/)
				yield return Uri.UnescapeDataString(enumerator.Current.Uri.AbsolutePath.Substring(container.Uri.LocalPath.Length + 1));
			}
		}

		private static string ComputeContentHash(Stream source)
		{
			byte[] hash;
			source.Seek(0, SeekOrigin.Begin);
			using (var md5 = MD5.Create())
			{
				hash = md5.ComputeHash(source);
			}

			source.Seek(0, SeekOrigin.Begin);
			return Convert.ToBase64String(hash);
		}

		/// <summary>
		/// Apply a content hash to the blob to verify upload and roundtrip consistency.
		/// </summary>
		private static void ApplyContentHash(CloudBlob blob, Stream stream)
		{
			var hash = ComputeContentHash(stream);
			blob.Properties.ContentMD5 = hash;
			blob.Metadata[MetadataMD5Key] = hash;
		}

		/// <summary>
		/// Throws a DataCorruptionException if the content hash is available but doesn't match.
		/// </summary>
		private static void VerifyContentHash(CloudBlob blob, Stream stream)
		{
			var expectedHash = blob.Metadata[MetadataMD5Key];
			if (string.IsNullOrEmpty(expectedHash))
			{
				return;
			}

			if (expectedHash != ComputeContentHash(stream))
			{
				throw new DataCorruptionException();
			}
		}

		public Result<string> TryAcquireLease(string containerName, string blobName)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			var blob = container.GetBlockBlobReference(blobName);
			var credentials = _blobStorage.Credentials;
			var uri = new Uri(credentials.TransformUri(blob.Uri.ToString()));
			var request = BlobRequest.Lease(uri, 90, LeaseAction.Acquire, null);
			credentials.SignRequest(request);

			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException we)
			{
				var statusCode = ((HttpWebResponse) we.Response).StatusCode;
				switch (statusCode)
				{
					case HttpStatusCode.Conflict:
					case HttpStatusCode.RequestTimeout:
					case HttpStatusCode.InternalServerError:
						return Result<string>.CreateError(statusCode.ToString());
					default:
						throw;
				}
			}

			try
			{
				return response.StatusCode == HttpStatusCode.Created
					? Result<string>.CreateSuccess(response.Headers["x-ms-lease-id"])
					: Result<string>.CreateError(response.StatusCode.ToString());
			}
			finally
			{
				response.Close();
			}
		}

		public bool TryReleaseLease(string containerName, string blobName, string leaseId)
		{
			return TryLeaseAction(containerName, blobName, LeaseAction.Release, leaseId);
		}

		public bool TryRenewLease(string containerName, string blobName, string leaseId)
		{
			return TryLeaseAction(containerName, blobName, LeaseAction.Renew, leaseId);
		}

		private bool TryLeaseAction(string containerName, string blobName, LeaseAction action, string leaseId = null)
		{
			var container = _blobStorage.GetContainerReference(containerName);
			var blob = container.GetBlockBlobReference(blobName);
			var credentials = _blobStorage.Credentials;
			var uri = new Uri(credentials.TransformUri(blob.Uri.ToString()));
			var request = BlobRequest.Lease(uri, 90, action, leaseId);
			credentials.SignRequest(request);

			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException we)
			{
				var statusCode = ((HttpWebResponse) we.Response).StatusCode;
				switch(statusCode)
				{
					case HttpStatusCode.Conflict:
					case HttpStatusCode.RequestTimeout:
					case HttpStatusCode.InternalServerError:
						return false;
					default:
						throw;
				}
			}

			try
			{
				var expectedCode = action == LeaseAction.Break ? HttpStatusCode.Accepted : HttpStatusCode.OK;
				return response.StatusCode == expectedCode;
			}
			finally
			{
				response.Close();
			}
		}
	}
}