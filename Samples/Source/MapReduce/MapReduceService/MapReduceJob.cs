#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cloud.Samples.MapReduce
{
	/// <summary>Entry point for setting up and consuming a map/reduce service.</summary>
	/// <typeparam name="TMapIn">The type of the items that are input in the map operation.</typeparam>
	/// <typeparam name="TMapOut">The type of the items that are output from the map operation.</typeparam>
	/// <remarks>All public members are thread-safe.</remarks>
	/// <seealso cref="MapReduceBlobSet"/>
	/// <seealso cref="MapReduceService"/>
	public sealed class MapReduceJob<TMapIn, TMapOut>
	{

		// HACK: thread-safety is achieved via locks. It would be better to make this class immutable.

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

			_jobName = Guid.NewGuid().ToString("N");
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
		/// <returns>The batch ID.</returns>
		/// <exception cref="InvalidOperationException">If the method was already called.</exception>
		/// <exception cref="ArgumentException">If <paramref name="items"/> contains less than two items.</exception>
		public string PushItems(IMapReduceFunctions functions, IList<TMapIn> items, int workerCount)
		{
			lock(_jobName)
			{
				if(_itemsPushed) throw new InvalidOperationException("A batch was already pushed to the work queue");

				var blobSet = new MapReduceBlobSet(_blobStorage, _queueStorage);
				blobSet.GenerateBlobSets(_jobName, new List<object>(from i in items select (object)i), functions, workerCount, typeof(TMapIn), typeof(TMapOut));
				_itemsPushed = true;

				return _jobName;
			}
		}

		/// <summary>Indicates whether the job is completed.</summary>
		/// <returns><c>true</c> if the batch is completed, <c>false</c> otherwise.</returns>
		public bool IsCompleted()
		{
			lock(_jobName)
			{
				var blobSet = new MapReduceBlobSet(_blobStorage, _queueStorage);

				var status = blobSet.GetCompletedBlobSets(_jobName);
				if(status.Item1 < status.Item2) return false;

				try
				{
					blobSet.GetAggregatedResult<object>(_jobName);
					return true;
				}
				catch(InvalidOperationException)
				{
					return false;
				}
			}
		}

		/// <summary>Gets the result of a job.</summary>
		/// <returns>The result item.</returns>
		/// <exception cref="InvalidOperationException">If the result is not ready (<seealso cref="M:IsCompleted"/>).</exception>
		public TMapOut GetResult()
		{
			lock(_jobName)
			{
				var blobSet = new MapReduceBlobSet(_blobStorage, _queueStorage);
				return blobSet.GetAggregatedResult<TMapOut>(_jobName);
			}
		}

		/// <summary>Deletes all the data related to the job.</summary>
		/// <remarks>After calling this method, the instance of <see cref="T:MapReduceJob"/> 
		/// should not be used anymore.</remarks>
		public void DeleteJobData()
		{
			lock(_jobName)
			{
				var blobSet = new MapReduceBlobSet(_blobStorage, _queueStorage);
				blobSet.DeleteJobData(_jobName);
			}
		}

	}

}
