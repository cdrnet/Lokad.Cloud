﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad;
using Lokad.Cloud;

namespace Lokad.Cloud.Samples.MapReduce
{
	
	/// <summary>Entry point for consuming a map/reduce service.</summary>
	/// <typeparam name="TMapIn">The type of the items that are input in the map operation.</typeparam>
	/// <typeparam name="TMapOut">The type of the items that are output from the map operation.</typeparam>
	/// <typeparam name="TReduceOut">The type of the items that are output from the reduce operation.</typeparam>
	/// <remarks>All public members are thread-safe.</remarks>
	public sealed class MapReduceJob<TMapIn, TMapOut, TReduceOut>
	{

		string _jobName;
		IBlobStorageProvider _blobStorage;
		IQueueStorageProvider _queueStorage;

		bool _itemsPushed = false;

		/// <summary>Initializes a new instance of the 
		/// <see cref="T:MapReduceJob{TMapIn,TMapOut,TReduceOut}"/> generic class.</summary>
		/// <param name="blobStorage">The blob storage provider.</param>
		/// <param name="queueStorage">The queue storage provider.</param>
		public MapReduceJob(IBlobStorageProvider blobStorage, IQueueStorageProvider queueStorage)
		{
			Enforce.Argument(() => blobStorage);
			Enforce.Argument(() => queueStorage);

			_jobName = Guid.NewGuid().ToString("M");
			_blobStorage = blobStorage;
			_queueStorage = queueStorage;
		}

		/// <summary>Initializes a new instance of the 
		/// <see cref="T:MapReduceJob{TMapIn,TMapOut,TReduceOut}"/> generic class.</summary>
		/// <param name="jobId">The ID of the job as previously returned by <see cref="M:PushItems"/>.</param>
		/// <param name="blobStorage">The blob storage provider.</param>
		/// <param name="queueStorage">The queue storage provider.</param>
		public MapReduceJob(string jobId, IBlobStorageProvider blobStorage, IQueueStorageProvider queueStorage)
		{
			Enforce.Argument(() => jobId);
			Enforce.Argument(() => blobStorage);
			Enforce.Argument(() => queueStorage);

			_jobName = jobId;
			_itemsPushed = true;
			_blobStorage = blobStorage;
			_queueStorage = queueStorage;
		}

		/// <summary>Pushes a batch of items for processing.</summary>
		/// <param name="functions">The functions for map/reduce/aggregate operations.</param>
		/// <param name="items">The items to process (at least two).</param>
		/// <param name="workerCount">The max number of workers to use.</param>
		/// <param name="maxDuration">The maximum duration of the map/reduce process.</param>
		/// <returns>The batch ID.</returns>
		/// <exception cref="InvalidOperationException">If the method was already called.</exception>
		/// <exception cref="ArgumentException">If <paramref name="items"/> contains less than two items.</exception>
		public string PushItems(MapReduceFunctions functions, IList<object> items, int workerCount, TimeSpan maxDuration)
		{
			lock(_jobName)
			{
				if(_itemsPushed) throw new InvalidOperationException("A batch was already pushed to the work queue");

				var blobSet = new MapReduceBlobSet(_blobStorage, _queueStorage);
				blobSet.GenerateBlobSets(_jobName, items, functions, workerCount, maxDuration);
				_itemsPushed = true;

				return _jobName;
			}
		}

		/// <summary>Indicates whether the job is completed.</summary>
		/// <returns><c>true</c> if the batch is completed, <c>false</c> otherwise.</returns>
		public bool IsCompleted()
		{
			throw new NotImplementedException();
		}

		/// <summary>Gets the result of a job whose output is a single item 
		/// (the aggregator function was specified).</summary>
		/// <returns>The result item.</returns>
		/// <exception cref="InvalidOperationException">If the aggregator function was not specified.</exception>
		public TReduceOut GetSingleResult()
		{
			// Verify exceptions
			throw new NotImplementedException();
		}

		/// <summary>Gets the result of a job whose output is a multiple items 
		/// (the aggregator function was not specified).</summary>
		/// <returns>The result items.</returns>
		/// <exception cref="InvalidOperationException">If the aggregator function was specified.</exception>
		public IEnumerable<TReduceOut> GetMultipleResults()
		{
			// Verify exceptions
			throw new NotImplementedException();
		}

	}

}
