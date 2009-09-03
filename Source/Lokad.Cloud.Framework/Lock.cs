#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

// TODO: [vermorel] 2009-07-23, excluded from the built
// Focusing on a minimal amount of feature for the v0.1
// will be reincluded later on.

// TODO: lock should be refreshed 'in the background' as long the process is running
// (too much a pain for the client to manually refresh the lock otherwise).

namespace Lokad.Cloud
{
	/// <summary><para>Cloud locks are used to synchronize access to resources.
	/// Application should associate a lock ID to a specific resource that
	/// forbids concurrent access; for example it's possible to have the
	/// lock ID 'bank-account' that regulates access to a bank account
	/// web service that does not support concurrent access.</para>
	/// <para>When the same lock is requested by multiple workers,
	/// the order of acquisition is not guaranteed.</para>
	/// <para>The scope of the lock is account-wide.</para></summary>
	/// <remarks>
	/// Typical usage pattern is:
	/// <code>
	/// using(var lk = Lock.TryAcquire("mylock"))
	/// {
	///		// Safe execution segment
	///		// ...
	///		// ... Expected execution time greater than 2 hours ...
	///		lk.Refresh();
	///		// ...
	/// }
	/// </code>
	/// </remarks>
	public sealed class Lock : IDisposable
	{	
		// Refresh issue:
		// Basically, a worker has let its lock expire and it now wants to refresh it; the following two cases can occur
		// 1. the lock was not requested by any other worker, so it's fine to refresh it
		// 2. the lock was stolen by another worker (which considered it expired) and therefore
		//    the original worker cannot do anything with it
		// This check can be done by keeping the date/time of the lock acquisition/refresh as a field and comparing it to the one
		// stored in the blob - if they are different, the lock cannot be refreshed but only disposed of (w/o deleting the blob)

		/// <summary>
		/// The name of the blob container used to store lock data.
		/// </summary>
		public static readonly string ContainerName = "lokad-cloud-locks";
		/// <summary>
		/// The minimum lock acquisition timeout, in milliseconds.
		/// </summary>
		public static readonly int MinimumTimeoutInMS = 100;
		/// <summary>
		/// The minimum lock duration, in milliseconds.
		/// </summary>
		public static readonly int MinimumLockDurationInMS = 500;

		const int IterationIntervalInMS = 500;

		// Locks are stored as blobs whose *contents* are:
		// - UTC date/time at which they are acquired/refreshed (yyMMddHHmmssFFF format, which is easily comparable)
		// - a pipe |
		// - the expected duration of the lock in ms
		const string LockDateTimeFormat = "yyMMddHHmmssFFF";

		/// <summary>
		/// The default lock duration.
		/// </summary>
		public static readonly TimeSpan DefaultLockDuration = new TimeSpan(2, 0, 0);

		private IBlobStorageProvider _provider;
		private int _expectedLockDurationInMS;
		private string _blobName;

		private DateTime _lastLockRefresh;

		/// <summary>Try to acquire the lock. Call is blocked until the lock is
		/// acquired.</summary>
		/// <param name="provider">The blob storage provider.</param>
		/// <param name="lockId">Unique lock identifier.</param>
		public Lock(IBlobStorageProvider provider, string lockId)
			: this(provider, lockId, TimeSpan.MaxValue, new TimeSpan(2, 0, 0))
		{
		}

		/// <summary>Try to acquire the lock with the specified timespan. If the lock
		/// could not be acquired a <see cref="TimeoutException" /> is thrown.</summary>
		public Lock(IBlobStorageProvider provider, string lockId, TimeSpan timeout, TimeSpan expectedLockDuration)
		{
			if (provider == null) throw new ArgumentNullException("provider");
			if (lockId == null) throw new ArgumentNullException("lockId");
			if (lockId.Length == 0) throw new ArgumentException("Lock ID cannot be an empty string", "lockId");
			if (timeout.TotalMilliseconds < MinimumTimeoutInMS)
			{
				throw new ArgumentException("Specified timeout is too short, must be greater than or equal to " +
					MinimumTimeoutInMS + "ms", "timeout");
			}
			if (expectedLockDuration.TotalMilliseconds < MinimumLockDurationInMS)
			{
				throw new ArgumentException(
					string.Format("Specified lock duration is too short, must be greater than or equal to {0}ms", 
									MinimumLockDurationInMS), "timeout");
			}

			// Strategy
			// 1. Create container if it does not exist
			// 2. Normalize the lock ID so that it is suitable as a blob name
			// 3. Detect whether the blob already exists
			//   3.1. If it exists
			//     3.1.1. Start iterating every IterationInterval ms, hoping to go to 3.2 before timeout
			//   3.2. If it does not exist, create the container with the specified name

			_provider = provider;
			_expectedLockDurationInMS = (int)Math.Round(expectedLockDuration.TotalMilliseconds);

			_blobName = NormalizeLockId(lockId);

			// False means that the container already exists, not that the creation failed
			_provider.CreateContainer(ContainerName);

			// TODO: wrong pattern, don't do 'canAcquire' followed by 'Acquire'
			// (it does not look atomic)
			if (CanAcquireLock()) AcquireLock();
			else
			{
				// Wait for lock to be released (until timeout)
				var waitBegin = DateTime.Now;
				bool waitAgain;
				do
				{
					System.Threading.Thread.Sleep(IterationIntervalInMS);
					waitAgain = !CanAcquireLock();

					if (waitAgain && (DateTime.Now - waitBegin > timeout))
					{
						throw new TimeoutException("Could not acquire the lock within the specified timeout");
					}
				} while (waitAgain);

				// Lock can now be acquired
				AcquireLock();
			}
		}

		/// <summary>
		/// Refreshes the lock.
		/// </summary>
		public void Refresh()
		{
			// TODO: lock should be refreshed 'in the background' as long the process is running
			// (too much a pain for the client to manually refresh the lock otherwise).

			// Refresh is like a "blind" lock acquisition
			if (CanAcquireLock())
			{
				AcquireLock();
			}
			else throw new InvalidOperationException("Lock was stolen by another worker");
		}

		/// <summary>
		/// Disposes of the current lock, releasing it.
		/// </summary>
		public void Dispose()
		{
			if (CanAcquireLock())
			{
				_provider.DeleteBlob(ContainerName, _blobName);
			}
		}

		/// <summary>
		/// Acquires the lock.
		/// </summary>
		private void AcquireLock()
		{
			// TODO: Use IBlobStorageProvider.UpdateIfNotModified here instead

			bool done;
			_lastLockRefresh = DateTime.Now.ToUniversalTime();
			done = _provider.PutBlob(ContainerName, _blobName,
				_lastLockRefresh.ToString(LockDateTimeFormat) +
				"|" + _expectedLockDurationInMS, true);

			if (!done) throw new InvalidOperationException("Could not store lock information");
		}

		/// <summary>
		/// Detects whether the lockcan be acquired.
		/// </summary>
		/// <returns><b>True</b> if the lock can be acquired, <b>false</b> otherwise.</returns>
		private bool CanAcquireLock()
		{
			string lockCreatedOnString;
			int expectedDurationInMs;
			var exists = ReadBlobContents(out lockCreatedOnString, out expectedDurationInMs);

			if(!exists) return true;
			
			// Lock acquisition date/time must equal the one in the blob
			var temp = _lastLockRefresh.ToString(LockDateTimeFormat);
			if (lockCreatedOnString.CompareTo(temp) <= 0) return true;

			// Check duration of the lock
			var maxPastDate = DateTime.Now.ToUniversalTime().AddMilliseconds(-expectedDurationInMs);
			var maxPastDateString = maxPastDate.ToString(LockDateTimeFormat);

			// If lockCreatedOnString < maxPastDateString, i.e. the lock was
			// acquired before Now-ExpectedDuration, assume the lock as inexistent
			return lockCreatedOnString.CompareTo(maxPastDateString) < 0;
		}

		/// <summary>
		/// Reads the contents of the blob associated to the lock.
		/// </summary>
		/// <param name="lockAcquisitionDateTimeString">The lock acquisition date/time (string).</param>
		/// <param name="expectedDurationInMs">The expected duration of the lock in ms.</param>
		/// <returns><b>True</b> if the blob exists, <b>false</b> otherwise.</returns>
		private bool ReadBlobContents(out string lockAcquisitionDateTimeString, out int expectedDurationInMs)
		{
			var content = _provider.GetBlob<string>(ContainerName, _blobName);

			if (content == null)
			{
				lockAcquisitionDateTimeString = null;
				expectedDurationInMs = 0;
				return false;
			}
			
			var pieces = content.Split('|');
			lockAcquisitionDateTimeString = pieces[0];
			expectedDurationInMs = int.Parse(pieces[1]);
			return true;
		}

		/// <summary>
		/// Normalizes the lock ID so that it is suitable as a blob name.
		/// </summary>
		/// <param name="lockId">The lock ID.</param>
		/// <returns>The normalized lock ID.</returns>
		private static string NormalizeLockId(string lockId)
		{
			// Blob names can be pretty much any character, just replace spaces
			return lockId.Replace(" ", "_");
		}


		/// <summary>
		/// Resolves the blob storage provider through IoC container.
		/// </summary>
		/// <returns>The blob storage provider.</returns>
		private static IBlobStorageProvider ResolveProvider()
		{
			// TODO: Resolve the blob storage provider through IoC
			throw new NotImplementedException();
		}

		/// <summary>
		/// Tries to acquire a lock with the default lock duration.
		/// </summary>
		/// <param name="lockId">The ID of the lock.</param>
		/// <returns>The lock object (dispose of properly).</returns>
		public static Lock TryAcquire(string lockId)
		{
			return new Lock(ResolveProvider(), lockId);
		}

		/// <summary>
		/// Tries to acquire a lock with the default lock duration.
		/// </summary>
		/// <param name="lockId">The ID of the lock.</param>
		/// <param name="timeout">The timeout. When expired without a lock being acquired,
		/// a <see cref="TimeoutException" /> is thrown.</param>
		/// <returns>The lock object (dispose of properly).</returns>
		public static Lock TryAcquire(string lockId, TimeSpan timeout)
		{
			return new Lock(ResolveProvider(), lockId, timeout, DefaultLockDuration);
		}

		/// <summary>
		/// Tries to acquire a lock.
		/// </summary>
		/// <param name="lockId">The ID of the lock.</param>
		/// <param name="timeout">The timeout. When expired without a lock being acquired,
		/// a <see cref="TimeoutException" /> is thrown.</param>
		/// <param name="expectedLockDuration">The expected duration of the lock.</param>
		/// <returns>The lock object (dispose of properly).</returns>
		public static Lock TryAcquire(string lockId, TimeSpan timeout, TimeSpan expectedLockDuration)
		{
			return new Lock(ResolveProvider(), lockId, timeout, expectedLockDuration);
		}

	}

}
