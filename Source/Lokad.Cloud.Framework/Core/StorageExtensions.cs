#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Text;
using System.Threading;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Core
{
	/// <summary>Helper extensions methods for storage providers.</summary>
	public static class StorageExtensions
	{
		static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();

		/// <summary>Delimiter used for prefixing iterations on Blob Storage.</summary>
		const string Delimiter = "/";

		static readonly Random _rand = new Random();

		// TODO: document those methods.
		// TODO: add missing AtomicUpdate overloads.

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Result<T>> updater, out Result<T> result)
		{
			Result<T> tmpResult = null;
			RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

			result = tmpResult;
		}

		public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, T> updater, out T result)
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
			// HACK: hard-coded constants, the whole counter system have to be perfected.
			const int InitMaxSleepInMs = 50;
			const int MaxSleepInMs = 2000;

			var maxSleepInMs = InitMaxSleepInMs;

			while (!func())
			{
				var sleepTime = _rand.Next(maxSleepInMs).Milliseconds();
				Thread.Sleep(sleepTime);

				maxSleepInMs += 50;
				maxSleepInMs = Math.Min(maxSleepInMs, MaxSleepInMs);
			}
		}

		///<summary>Get a pseudo-random pattern that can be used to facilitate
		/// parallel iteration.</summary>
		public static string GetHashPrefix(int hexDepth)
		{
			var builder = new StringBuilder();

			for (int i = 0; i < hexDepth; i++)
			{
				builder.Append(HexDigits[_rand.Next(16)]);
				if(i < hexDepth - 1) builder.Append(Delimiter);
			}

			return builder.ToString();
		}

		// TODO: add missing overload for 'BaseBlobName'.

		public static T GetBlob<T>(this IBlobStorageProvider provider, BaseBlobName fullName)
		{
			return provider.GetBlob<T>(fullName.ContainerName, fullName.ToString());
		}

		public static void PutBlob<T>(this IBlobStorageProvider provider, BaseBlobName fullName, T item)
		{
			provider.PutBlob(fullName.ContainerName, fullName.ToString(), item);
		}

		public static bool PutBlob<T>(this IBlobStorageProvider provider, BaseBlobName fullName, T item, bool overwrite)
		{
			return provider.PutBlob(fullName.ContainerName, fullName.ToString(), item, overwrite);
		}
	}
}
