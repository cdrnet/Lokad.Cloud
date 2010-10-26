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

namespace Lokad.Cloud.Storage
{
	/// <summary>Helper extensions methods for storage providers.</summary>
	public static class StorageExtensions
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

		/// <summary>Gets messages from a queue with a visibility timeout of 2 hours and a maximum of 50 processing trials.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue to be pulled.</param>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <returns>Enumeration of messages, possibly empty.</returns>
		public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, string queueName, int count)
		{
			return provider.Get<T>(queueName, count, new TimeSpan(2, 0, 0), 50);
		}

		/// <summary>Gets messages from a queue with a visibility timeout of 2 hours.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue to be pulled.</param>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <param name="maxProcessingTrials">
		/// Maximum number of message processing trials, before the message is considered as
		/// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
		/// </param>
		/// <returns>Enumeration of messages, possibly empty.</returns>
		public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, string queueName, int count, int maxProcessingTrials)
		{
			return provider.Get<T>(queueName, count, new TimeSpan(2, 0, 0), maxProcessingTrials);
		}

		/// <summary>Gets the specified cloud entity if it exists.</summary>
		/// <typeparam name="T"></typeparam>
		public static Maybe<CloudEntity<T>> Get<T>(this ITableStorageProvider provider, string tableName, string partitionName, string rowKey)
		{
			return provider.Get<T>(tableName, partitionName, new[] { rowKey }).FirstOrEmpty();
		}

		/// <summary>Gets a strong typed wrapper around the table storage provider.</summary>
		public static CloudTable<T> GetTable<T>(this ITableStorageProvider provider, string tableName)
		{
			return new CloudTable<T>(provider, tableName);
		}

		/// <summary>Updates a collection of existing entities into the table storage.</summary>
		/// <remarks>
		/// <para>The call is expected to fail on the first non-existing entity. 
		/// Results are not garanteed if one or several entities do not exist already.
		/// </para>
		/// <para>The call is also expected to fail if one or several entities have
		/// changed remotely in the meantime. Use the overloaded method with the additional
		/// force parameter to change this behavior if needed.
		/// </para>
		/// <para>There is no upper limit on the number of entities provided through
		/// the enumeration. The implementations are expected to lazily iterates
		/// and to create batch requests as the move forward.
		/// </para>
		/// <para>Idempotence of the implementation is required.</para>
		/// </remarks>
		/// <exception cref="InvalidOperationException"> thrown if the table does not exist
		/// or an non-existing entity has been encountered.</exception>
		public static void Update<T>(this ITableStorageProvider provider, string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			provider.Update(tableName, entities, false);
		}

		/// <summary>Deletes a collection of entities.</summary>
		/// <remarks>
		/// <para>
		/// The implementation is expected to lazily iterate through all row keys
		/// and send batch deletion request to the underlying storage.</para>
		/// <para>Idempotence of the method is required.</para>
		/// <para>The method should not fail if the table does not exist.</para>
		/// <para>The call is expected to fail if one or several entities have
		/// changed remotely in the meantime. Use the overloaded method with the additional
		/// force parameter to change this behavior if needed.
		/// </para>
		/// </remarks>
		public static void Delete<T>(this ITableStorageProvider provider, string tableName, IEnumerable<CloudEntity<T>> entities)
		{
			provider.Delete(tableName, entities, false);
		}

        /// <summary>Checks that containerName is a valid DNS name, as requested by Azure</summary>
        public static bool IsContainerNameValid(string containerName)
        {
            return (Regex.IsMatch(containerName, @"(^([a-z]|\d))((-([a-z]|\d)|([a-z]|\d))+)$")
                && (3 <= containerName.Length) && (containerName.Length <= 63));
        }
	}
}
