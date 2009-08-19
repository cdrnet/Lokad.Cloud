#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Threading;
using Lokad.Cloud.Core;

namespace Lokad.Cloud.Framework
{
	/// <summary>Simple non-sharded counter.</summary>
	/// <remarks>The content of the counter is stored in a single blob value.</remarks>
	public class BlobCounter
	{
		static Random _rand = new Random();

		readonly IBlobStorageProvider _provider;

		readonly string _containerName;
		readonly string _blobName;

		/// <summary>Container that is storing the counter.</summary>
		public string ContainerName { get { return _containerName; } }

		/// <summary>Blob that is storing the counter.</summary>
		public string BlobName { get { return _blobName; } }

		/// <summary>Shorthand constructors.</summary>
		public BlobCounter(ProvidersForCloudStorage providers, string containerName, string blobName)
			: this(providers.BlobStorage, containerName, blobName)
		{
			
		}

		/// <summary>Full constructor.</summary>
		public BlobCounter(IBlobStorageProvider provider, string containerName, string blobName)
		{
			Enforce.Argument(() => provider);
			Enforce.Argument(() => containerName);
			Enforce.Argument(() => blobName);

			_provider = provider;
			_containerName = containerName;
			_blobName = blobName;
		}

		/// <summary>Returns the value of the counter.</summary>
		public decimal GetValue()
		{
			return _provider.GetBlob<decimal>(_containerName, _blobName);
		}

		/// <summary>Atomic increment the counter value.</summary>
		/// <remarks>If the counter does not exist before hand, it gets created with a zero value.</remarks>
		public decimal Increment(decimal increment)
		{
			var counter = decimal.MaxValue; // dummy initialization

			RetryUpdate(() => _provider.UpdateIfNotModified(
				_containerName,
				_blobName,
				x => x + increment, out counter));

			return counter;
		}

		/// <summary>Reset the counter at the given value.</summary>
		public void Reset(decimal value)
		{
			_provider.PutBlob(_containerName, _blobName, value);
		}

		/// <summary>Deletes the counter.</summary>
		/// <returns><c>true</c> if the counter has actually been deleted by the call,
		/// and <c>false</c> otherwise.</returns>
		public bool Delete()
		{
			return _provider.DeleteBlob(_containerName, _blobName);
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
	}
}
