#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

// TODO: [vermorel] instanciation pattern to access the storage providers is still unclear.

// TODO: [vermorel] 2009-07-23, excluded from the built
// Focusing on a minimal amount of feature for the v0.1
// will be reincluded later on.

// TODO: [vermorel] DO NOT assume the Azure worker to be single threaded. 
// - 1) there can be multiple threads on a single processor.
// - 2) multi-procs instances are on their way to Windows Azure.

namespace Lokad.Cloud
{
	/// <summary>Compact unique identier class that provides unique values (incremented) in 
	/// a scalable manner. Compared to <see cref="Guid"/>, this class is intended as a way 
	/// to provide much more compact identifier.</summary>
	/// <remarks>
	/// <para>Scalability is achieved through an exponential identifier allocation pattern.</para>
	/// <para>All members of the <see cref="Cuid" /> class are <b>not</b> thread-safe 
	/// (Azure workers are single-threaded).</para>
	/// </remarks>
	public static class Cuid
	{
		// Note: blobs contain the first available ID (binary format),
		// so when a counter is requested for the very first time,
		// a blob is created with content '2' because '1' is issued right away

		/// <summary>
		/// The name of the container used to store CUID data.
		/// </summary>
		public const string ContainerName = "lokad-cloud-cuids";

		static readonly TimeSpan DefaultLockDuration = 15.Seconds();

		// 10 (initial) counters should be enough for everyone
		static readonly Dictionary<string, Counter> AllocatedCuids = new Dictionary<string, Counter>(10);

		/// <summary>Returns a unique identifier.</summary>
		/// <param name="counterId">Name of the counter being incremented.</param>
		/// <returns>This value is unique and won't be returned any other Azure
		/// role that request the value.</returns>
		/// <remarks>
		/// If the counter does not exist, it gets created by the first call.
		/// Counter returns integers that are strictly increasing. If
		/// there are concurrent call the counter may skip values between calls
		/// (do not expect a small guaranteed <c>+1</c> increment each time the
		/// counter is increased).
		/// </remarks>
		public static long Next(string counterId)
		{
			return Next(ResolveProvider(), counterId);
		}

		public static long Next(IBlobStorageProvider provider, string counterId)
		{
			if (provider == null) throw new ArgumentNullException("provider");
			if (counterId == null) throw new ArgumentNullException("counterId");
			if (counterId.Length == 0) throw new ArgumentException("Counter ID cannot be empty", "counterId");

			// Strategy
			// 1. Verify whether the counter is already in cache (dictionary)
			//   1.1. If so, verify whether there are free IDs to issue
			//     1.1.1. If there is a free ID, issue it and update Counter info in memory
			//	   1.1.2. If not, load a block of IDs from blob and update it (using lock),
			//            then go to 1.1.1 and return
			//   1.2. If not, verify blob existence in disk (using lock)
			//     1.2.1. If the blob exists, go to 1.1.2 and return
			//     1.2.2. If not, create the blob with "initial" data and return 1 (using lock)

			var blobName = counterId;
			Counter currentCounter;

			// False means that the container already exists, not that the creation failed
			provider.CreateContainer(ContainerName);

			if (AllocatedCuids.TryGetValue(counterId, out currentCounter))
			{
				// 1.1 - counter is cached
				if (currentCounter.AllocatedCuids > 0)
				{
					// 1.1.1 - ID available
					return UpdateCounterAndReturnResult(currentCounter);
				}
				
				// 1.1.2 - ID not available, must update blob
				PerformAllocationWithLock(provider, counterId, blobName, currentCounter);

				return UpdateCounterAndReturnResult(currentCounter);
			}
			
			// 1.2 - no cached counter
			using (new Lock(provider, GetLockId(counterId), TimeSpan.MaxValue, DefaultLockDuration))
			{
				var firstFreeId = provider.GetBlob<long>(ContainerName, blobName);
				if (firstFreeId > 0)
				{
					// 1.2.1 - same as 1.1.2
					currentCounter = new Counter
						{
							AllocatedCuids = 0,
							AllocationCount = 0,
							FirstFreeCuid = firstFreeId
						};
					PerformAllocation(provider, counterId, blobName, currentCounter);
					AllocatedCuids.Add(counterId, currentCounter);

					return UpdateCounterAndReturnResult(currentCounter);
				}
				
				// 1.2.2 - create blob with standard data, update cache and issue 1
				currentCounter = new Counter
					{
						AllocatedCuids = 0,
						AllocationCount = 1,
						FirstFreeCuid = 1
					};
				AllocatedCuids.Add(counterId, currentCounter);
				provider.PutBlob<long>(ContainerName, blobName, 2);
				return 1;
			}
		}

		/// <summary>
		/// Performs CUID allocation, internally acquiring a lock.
		/// </summary>
		private static void PerformAllocationWithLock(
			IBlobStorageProvider provider, string counterId, string blobName, Counter counter)
		{
			// TODO: do not use a Lock here, go directly for atomic update
			using (new Lock(provider, GetLockId(counterId), TimeSpan.MaxValue, DefaultLockDuration))
			{
				PerformAllocation(provider, counterId, blobName, counter);
			}
		}

		/// <summary>
		/// Performs CUID allocation.
		/// </summary>
		private static void PerformAllocation(IBlobStorageProvider provider, string counterId, string blobName, Counter counter)
		{
			// Strategy
			// 1. Load content of the blob (if it's zero, throw)
			// 2. Allocate the appropriate number of CUIDs (2^AllocationCount++)
			// 3. Update counter
			// 4. Update blob with the first free CUID (which is current_from_blob + num_of_allocated_cuids)

			var firstFreeCuid = provider.GetBlob<long>(ContainerName, blobName);
			if (firstFreeCuid <= 0) throw new InvalidOperationException("The blob is supposed to contain a value greater than zero");

			counter.AllocationCount++; 
			counter.AllocatedCuids = (long)Math.Pow(2, counter.AllocationCount);
			counter.FirstFreeCuid = firstFreeCuid;

			if (long.MaxValue - firstFreeCuid < counter.AllocatedCuids) throw new InvalidOperationException("CUID space on counter " + counterId + " is exhausted");
			firstFreeCuid += counter.AllocatedCuids;
			// Update by overwriting (resource already locked)
			var updated = provider.PutBlob(ContainerName, blobName, firstFreeCuid, true);
			if (!updated) throw new InvalidOperationException("Could not update blob content");
		}

		/// <summary>
		/// Updates a Counter for issuing a CUID and then issues it.
		/// </summary>
		/// <param name="currentCounter">The counter.</param>
		/// <returns>The issued CUID.</returns>
		private static long UpdateCounterAndReturnResult(Counter currentCounter)
		{
			if (currentCounter.AllocatedCuids <= 0) throw new InvalidOperationException("No available CUID");

			var result = currentCounter.FirstFreeCuid;
			currentCounter.FirstFreeCuid++;
			currentCounter.AllocatedCuids--;
			return result;
		}
		
		/// <summary>
		/// Gets the ID of the lock given the counter ID.
		/// </summary>
		/// <param name="counterId">The counter ID.</param>
		/// <returns>The lock ID.</returns>
		private static string GetLockId(string counterId)
		{
			return ContainerName + "-" + counterId;
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
		/// Contains information about a counter status.
		/// </summary>
		class Counter
		{
			/// <summary>
			/// Gets or sets the first available CUID.
			/// </summary>
			internal long FirstFreeCuid { get; set; }

			/// <summary>
			/// Gets or sets the number of allocated CUIDs.
			/// </summary>
			internal long AllocatedCuids { get; set; }

			/// <summary>
			/// Gets or sets the number of allocations done so far.
			/// </summary>
			internal int AllocationCount { get; set; }
		}

	}

}
