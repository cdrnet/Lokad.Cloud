#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
		/// increases to avoid overstressing the storage for nothing.</summary>
		/// <param name="func"></param>
		static void RetryUpdate(Func<bool> func)
		{
			// HACK: hard-coded constant, the whole counter system have to be perfected.
			const int MaxSleepInMs = 50;

			while (!func())
			{
				var sleepTime = _rand.Next(MaxSleepInMs).Milliseconds();
				Thread.Sleep(sleepTime);
			}
		}

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobReference<T> reference, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			AtomicUpdate(provider, reference.ContainerName, reference.ToString(), updater, out result);
		}

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobReference<T> reference, Func<Maybe<T>, T> updater, out T result)
		{
			AtomicUpdate(provider, reference.ContainerName, reference.ToString(), updater, out result);
		}

		public static bool DeleteBlob(this IBlobStorageProvider provider, BlobName fullName)
		{
			return provider.DeleteBlob(fullName.ContainerName, fullName.ToString());
		}

		public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobReference<T> reference)
		{
			return provider.GetBlob<T>(reference.ContainerName, reference.ToString());
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
		public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, BlobReference<T> reference)
		{
			return provider.GetBlobOrDelete<T>(reference.ContainerName, reference.ToString());
		}

		public static void PutBlob<T>(this IBlobStorageProvider provider, BlobReference<T> reference, T item)
		{
			provider.PutBlob(reference.ContainerName, reference.ToString(), item);
		}

		public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobReference<T> reference, T item, bool overwrite)
		{
			return provider.PutBlob(reference.ContainerName, reference.ToString(), item, overwrite);
		}

		public static IEnumerable<string> List<N>(this IBlobStorageProvider provider, string prefix) 
			where N : BlobName
		{
			return provider.List(BlobName.GetContainerName<N>(), prefix);
		}

		public static IEnumerable<T> List<T>(
			this IBlobStorageProvider provider, BlobNamePrefix<T> prefix) where T : BlobName
		{
			return provider.List(prefix.Container, prefix.Prefix)
				.Select(rawName => BlobName.Parse<T>(rawName));
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobReference<T> reference, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
		{
			return provider.UpdateIfNotModified(reference.ContainerName, reference.ToString(), updater, out result);
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobReference<T> reference, Func<Maybe<T>, T> updater, out T result)
		{
			return provider.UpdateIfNotModified(reference.ContainerName, reference.ToString(), updater, out result);
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobReference<T> reference, Func<Maybe<T>, Result<T>> updater)
		{
			return provider.UpdateIfNotModified(reference.ContainerName, reference.ToString(), updater);
		}

		public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
			BlobReference<T> reference, Func<Maybe<T>, T> updater)
		{
			return provider.UpdateIfNotModified(reference.ContainerName, reference.ToString(), updater);
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
			return provider.Get<T>(tableName, partitionName, new[] {rowKey}).FirstOrEmpty();
		}

		/// <summary>Gets a strong typed wrapper around the table storage provider.</summary>
		public static CloudTable<T> GetTable<T>(this ITableStorageProvider provider, string tableName)
		{
			return new CloudTable<T>(provider, tableName);
		}
	}
}