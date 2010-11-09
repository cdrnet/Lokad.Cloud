#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cloud.Storage.Blobs
{
	/// <summary>Helper extensions methods for storage providers.</summary>
	public static class BlobStorageExtensions
	{
		static readonly Random _rand = new Random();

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			Result<T> tmpResult = null;
			RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

			result = tmpResult;
		}

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>, T> updater, out T result)
		{
			T tmpResult = default(T);
			RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

			result = tmpResult;
		}

		/// <summary>Retry an update method until it succeeds. Timing
		/// increases to avoid overstressing the storage for nothing. 
		/// Maximal delay is set to 10 seconds.</summary>
		static void RetryUpdate(Func<bool> func)
		{
			// HACK: hard-coded constant, the whole counter system have to be perfected.
			const int step = 10;
			const int maxDelayInMilliseconds = 10000;

			int retryAttempts = 0;
			while (!func())
			{
				retryAttempts++;
				var sleepTime = _rand.Next(Math.Max(retryAttempts * retryAttempts * step, maxDelayInMilliseconds)).Milliseconds();
				Thread.Sleep(sleepTime);

			}
		}

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			AtomicUpdate(provider, name.ContainerName, name.ToString(), updater, out result);
		}

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>, T> updater, out T result)
		{
			AtomicUpdate(provider, name.ContainerName, name.ToString(), updater, out result);
		}

		public static bool DeleteBlob<T>(this IBlobStorageProvider provider, BlobName<T> fullName)
		{
			return provider.DeleteBlob(fullName.ContainerName, fullName.ToString());
		}

		public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobName<T> name)
		{
			return provider.GetBlob<T>(name.ContainerName, name.ToString());
		}

		public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, out string etag)
		{
			return provider.GetBlob<T>(name.ContainerName, name.ToString(), out etag);
		}

		public static string GetBlobEtag<T>(this IBlobStorageProvider provider, BlobName<T> name)
		{
			return provider.GetBlobEtag(name.ContainerName, name.ToString());
		}

		/// <summary>Gets the corresponding object. If the deserialization fails
		/// just delete the existing copy.</summary>
		public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName)
		{
			try
			{
				return provider.GetBlob<T>(containerName, blobName);
			}
			catch (SerializationException)
			{
				provider.DeleteBlob(containerName, blobName);
				return Maybe<T>.Empty;
			}
			catch (InvalidCastException)
			{
				provider.DeleteBlob(containerName, blobName);
				return Maybe<T>.Empty;
			}
		}

		/// <summary>Gets the corresponding object. If the deserialization fails
		/// just delete the existing copy.</summary>
		public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name)
		{
			return provider.GetBlobOrDelete<T>(name.ContainerName, name.ToString());
		}

		public static void PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item)
		{
			provider.PutBlob(name.ContainerName, name.ToString(), item);
		}

		public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item, bool overwrite)
		{
			return provider.PutBlob(name.ContainerName, name.ToString(), item, overwrite);
		}

		/// <summary>Push the blob only if etag is matching the etag of the blob in BlobStorage</summary>
		public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item, string etag)
		{
			return provider.PutBlob(name.ContainerName, name.ToString(), item, etag);
		}

		public static IEnumerable<T> List<T>(
			this IBlobStorageProvider provider, T prefix) where T : UntypedBlobName
		{
			return provider.List(prefix.ContainerName, prefix.ToString())
				.Select(rawName => UntypedBlobName.Parse<T>(rawName));
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobName<T> name, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater, out result);
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobName<T> name, Func<Maybe<T>, T> updater, out T result)
		{
			return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater, out result);
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobName<T> name, Func<Maybe<T>, Result<T>> updater)
		{
			return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater);
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobName<T> name, Func<Maybe<T>, T> updater)
		{
			return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater);
		}

		/// <summary>Checks that containerName is a valid DNS name, as requested by Azure</summary>
		public static bool IsContainerNameValid(string containerName)
		{
			return (Regex.IsMatch(containerName, @"(^([a-z]|\d))((-([a-z]|\d)|([a-z]|\d))+)$")
				&& (3 <= containerName.Length) && (containerName.Length <= 63));
		}
	}
}
