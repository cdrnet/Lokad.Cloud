#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud;
using Lokad;
using Lokad.Quality;

namespace Lokad.Cloud.Samples.MapReduce
{
	
	/// <summary>Manages sets of blobs for map/reduce services.</summary>
	/// <typeparam name="TMapIn">The type of the items that are input in the map operation.</typeparam>
	/// <typeparam name="TMapOut">The type of the items that are output from the map operation.</typeparam>
	/// <typeparam name="TReduceOut">The type of the items that are output from the reduce operation.</typeparam>
	/// <remarks>All public mebers are thread-safe.</remarks>
	public sealed class MapReduceBlobSet
	{
		/// <summary>The queue used for managing map/reduce work items (<seealso cref="T:BatchMessage"/>).</summary>
		internal const string JobsQueueName = "lokad-blobsets-queue";

		internal const string ConfigPrefix = "blobsets-config";
		internal const string InputPrefix = "blobsets-input";
		internal const string ReducedPrefix = "blobsets-reduced";
		internal const string AggregatedPrefix = "blobsets-aggregated";
		internal const string CounterPrefix = "blobsets-counter";

		// Final blob names:
		// - .../blobsets-config/<job-name> -- map/reduce/aggregate functions plus number of queued blobsets -- readonly
		// - .../blobsets-input/<job-name>/<blob-guid>
		// - .../blobsets-reduced/<job-name>/<blob-guid>
		// - .../blobsets-aggregated/<job-name>
		// - .../blobsets-counter/<job-name>

		IBlobStorageProvider _blobStorage;
		IQueueStorageProvider _queueStorage;

		/// <summary>Initializes a new instance of the <see cref="T:MapReduceBlobSet"/> generic class.</summary>
		/// <param name="blobStorage">The blob storage provider.</param>
		/// <param name="queueStorage">The queue storage provider.</param>
		public MapReduceBlobSet(IBlobStorageProvider blobStorage, IQueueStorageProvider queueStorage)
		{
			Enforce.Argument(() => blobStorage);
			Enforce.Argument(() => queueStorage);

			_blobStorage = blobStorage;
			_queueStorage = queueStorage;
		}

		/// <summary>Generates the blob sets that are required to run cloud-based map/reduce operations.</summary>
		/// <param name="jobName">The name of the job (should be unique).</param>
		/// <param name="items">The items that must be processed (at least two).</param>
		/// <param name="functions">The map/reduce/aggregate functions (aggregate is optional).</param>
		/// <param name="workerCount">The number of workers to use.</param>
		/// <param name="maxDuration">The maximum duration of the map/reduce/aggregate batch.</param>
		/// <exception cref="ArgumentException">If <paramref name="items"/> contains less than two items.</exception>
		/// <remarks>This method should be called from <see cref="T:MapReduceJob"/>.</remarks>
		public void GenerateBlobSets(string jobName, IList<object> items, MapReduceFunctions functions, int workerCount, TimeSpan maxDuration)
		{
			// Note: items is IList and not IEnumerable because the number of items must be known up-front

			// 1. Generate the blobsets
			// 2. Store config
			// 3. Put messages in the work queue

			int itemCount = items.Count;
			if(itemCount <= 2) throw new ArgumentException("items should contain at least two elements", "items");

			// Note: each blobset should contain at least two elements

			int blobSetCount = Math.Min(workerCount, (int)Math.Floor(itemCount / 2D));
			float blobsPerSet = (float)itemCount / (float)blobSetCount;

			// 1.1 Allocate blobsets
			var allNames = new InputBlobName[blobSetCount][];
			int processedBlobs = 0;
			for(int currSet = 0; currSet < blobSetCount; currSet++)
			{
				int thisSetSize = (int)Math.Ceiling(blobsPerSet);

				// Last blobset might be smaller
				if(currSet == blobSetCount - 1) allNames[currSet] = new InputBlobName[itemCount - processedBlobs];
				else allNames[currSet] = new InputBlobName[thisSetSize];

				processedBlobs += thisSetSize;
			}
			Enforce.That(processedBlobs == itemCount, "Processed Blobs are less than the number of items");

			// 1.2 Store input data (separate cycle for clarity)
			processedBlobs = 0;
			for(int currSet = 0; currSet < blobSetCount; currSet++)
			{
				for(int i = 0; i < allNames[currSet].Length; i++)
				{
					// BlobSet and Blob IDs start from zero (see step 3)
					allNames[currSet][i] = GetInputBlobName(jobName, maxDuration, currSet, i);

					var item = items[processedBlobs];
					_blobStorage.PutBlob(allNames[currSet][i], item);
					processedBlobs++;
				}
			}

			// 2. Store configuration
			var configBlobName = new MapReduceConfigurationName(jobName, maxDuration);
			_blobStorage.PutBlob(configBlobName, new MapReduceConfiguration()
				{ MapReduceFunctions = functions, BlobSetCount = blobSetCount });

			// 3. Queue messages
			for(int i = 0; i < blobSetCount; i++)
			{
				_queueStorage.Put(JobsQueueName, new JobMessage()
					{ Type = MessageType.BlobSetToProcess, JobName = jobName, BlobSetId = i });
			}
		}

		/// <summary>Performs map/reduce operations on a blobset.</summary>
		/// <param name="jobName">The name of the job.</param>
		/// <param name="blobSetId">The blobset ID.</param>
		/// <remarks>This method should be called from <see cref="T:MapReduceService"/>.</remarks>
		public void PerformMapReduce(string jobName, int blobSetId)
		{
			// 1. Load config
			// 2. For all blobs in blobset, do map (output N)
			// 3. For all mapped items, do reduce (output 1)
			// 4. Store reduce result
			// 5. Update counter
			// 6. If aggregator != null && blobsets are all processed --> enqueue aggregation message
			// 7. Delete blobset

			// 1. Load config
			var configBlobPrefix = MapReduceConfigurationName.GetPrefix(jobName);
			var config = _blobStorage.GetBlob<MapReduceConfiguration>(_blobStorage.List(configBlobPrefix).First());

			var blobsetPrefix = InputBlobName.GetPrefix(jobName, blobSetId);
			DateTime jobExpiration = DateTime.MinValue;
			var mapResults = new List<object>();
			
			// 2. Do map for all blobs in the blobset
			foreach(var blobName in _blobStorage.List(blobsetPrefix))
			{
				object inputBlob = _blobStorage.GetBlob<object>(blobName);
				jobExpiration = blobName.Expiration;

				object mapResult = InvokeAsDelegate(config.MapReduceFunctions.Mapper, inputBlob);
				mapResults.Add(mapResult);
			}

			// 3. Do reduce for all mapped results
			while(mapResults.Count > 1)
			{
				object item1 = mapResults[mapResults.Count - 1];
				object item2 = mapResults[mapResults.Count - 2];
				mapResults.RemoveAt(mapResults.Count - 1);
				mapResults.RemoveAt(mapResults.Count - 1);

				object reduceResult = InvokeAsDelegate(config.MapReduceFunctions.Reducer, item1, item2);
				mapResults.Add(reduceResult);
			}

			// 4. Store reduced result
			var reducedBlobName = GetReducedBlobName(jobName, jobExpiration - DateTime.Now, blobSetId);
			_blobStorage.PutBlob<object>(reducedBlobName, mapResults[0]);

			// 5. Update counter
			var counterName = GetCounterName(jobName, jobExpiration - DateTime.Now);
			var counter = new BlobCounter(_blobStorage, counterName);
			var totalCompletedBlobSets = (int)counter.Increment(1);
			
			// 6. Queue aggregation if appropriate
			if(config.MapReduceFunctions.Aggregator != null && totalCompletedBlobSets == config.BlobSetCount)
			{
				_queueStorage.Put(JobsQueueName, new JobMessage()
					{ JobName = jobName, BlobSetId = null, Type = MessageType.ReducedDataToAggregate });
			}

			// 7. Delete blobset's blobs
			foreach(var blobName in _blobStorage.List(blobsetPrefix))
			{
				_blobStorage.DeleteBlob(blobName);
			}
		}

		/// <summary>Performs the aggregate operation on a blobset.</summary>
		/// <param name="jobName">The name of the job.</param>
		public void PerformAggregate(string jobName)
		{
			// 1. Load config
			// 2. Do aggregation
			// 3. Store result
			// 4. Delete reduced data

			// 1. Load config
			var configBlobPrefix = MapReduceConfigurationName.GetPrefix(jobName);
			var config = _blobStorage.GetBlob<MapReduceConfiguration>(_blobStorage.List(configBlobPrefix).First());

			var reducedBlobPrefix = ReducedBlobName.GetPrefix(jobName);
			DateTime jobExpiration = DateTime.MinValue;
			var aggregateResults = new List<object>();

			// 2. Load reduced items and do aggregation
			foreach(var blobName in _blobStorage.List(reducedBlobPrefix))
			{
				jobExpiration = blobName.Expiration;
				aggregateResults.Add(_blobStorage.GetBlob<object>(blobName));
			}

			while(aggregateResults.Count > 1)
			{
				object item1 = aggregateResults[aggregateResults.Count - 1];
				object item2 = aggregateResults[aggregateResults.Count - 2];
				aggregateResults.RemoveAt(aggregateResults.Count - 1);
				aggregateResults.RemoveAt(aggregateResults.Count - 1);

				object aggregResult = InvokeAsDelegate(config.MapReduceFunctions.Aggregator, item1, item2);
				aggregateResults.Add(aggregResult);
			}

			// 3. Store aggregated result
			var aggregatedBlobName = GetAggregatedBlobName(jobName, jobExpiration - DateTime.Now);
			_blobStorage.PutBlob(aggregatedBlobName, aggregateResults[0]);

			// 4. Delete reduced data
			foreach(var blobName in _blobStorage.List(reducedBlobPrefix))
			{
				_blobStorage.DeleteBlob(blobName);
			}
		}

		#region Delegate Utils

		/// <summary>Use reflection to invoke a delegate.</summary>
		static object InvokeAsDelegate(object target, params object[] inputs)
		{
			return target.GetType().InvokeMember(
				"Invoke", System.Reflection.BindingFlags.InvokeMethod, null, target, inputs);
		}

		#endregion

		#region Name Utils

		static MapReduceConfigurationName GetConfigurationBlobName(string jobName, TimeSpan duration)
		{
			return new MapReduceConfigurationName(jobName, duration);
		}

		static InputBlobName GetInputBlobName(string jobName, TimeSpan duration, int blobSetId, int blobId)
		{
			return new InputBlobName(jobName, duration, blobSetId, blobId);
		}

		static ReducedBlobName GetReducedBlobName(string jobName, TimeSpan duration, int blobSetId)
		{
			return new ReducedBlobName(jobName, duration, blobSetId);
		}

		static AggregatedBlobName GetAggregatedBlobName(string jobName, TimeSpan duration)
		{
			return new AggregatedBlobName(jobName, duration);
		}

		static BlobCounterName GetCounterName(string jobName, TimeSpan duration)
		{
			return new BlobCounterName(jobName, duration);
		}

		#endregion

		#region Private Classes

		/// <summary>Contains configuration for a map/reduce job.</summary>
		[Serializable]
		public class MapReduceConfiguration
		{

			/// <summary>The map/reduce/aggregate functions.</summary>
			public MapReduceFunctions MapReduceFunctions { get; set; }

			/// <summary>The number of blobsets to be processed.</summary>
			public int BlobSetCount { get; set; }

		}

		public class MapReduceConfigurationName : BaseTemporaryBlobName<MapReduceConfiguration>
		{

			[UsedImplicitly, Rank(2)]
			public string JobName;

			public MapReduceConfigurationName(string jobName, TimeSpan duration)
				: base(DateTime.Now.Add(duration), MapReduceBlobSet.ConfigPrefix)
			{
				JobName = jobName;
			}

			public static BlobNamePrefix<MapReduceConfigurationName> GetPrefix(string jobName)
			{
				return GetPrefix(new MapReduceConfigurationName(jobName, 0.Minutes()), 3);
			}

		}

		private class InputBlobName : BaseTemporaryBlobName<object>
		{

			[UsedImplicitly, Rank(2)]
			public string JobName;
			[UsedImplicitly, Rank(3)]
			public int BlobSetId;
			[UsedImplicitly, Rank(4)]
			public int BlobId;

			public InputBlobName(string jobName, TimeSpan duration, int blobSetId, int blobId)
				: base(DateTime.Now.Add(duration), MapReduceBlobSet.InputPrefix)
			{
				JobName = jobName;
				BlobSetId = blobSetId;
				BlobId = blobId;
			}

			public static BlobNamePrefix<InputBlobName> GetPrefix(string jobName, int blobSetId)
			{
				return GetPrefix(new InputBlobName(jobName, 0.Minutes(), blobSetId, 0), 4);
			}

		}

		private class ReducedBlobName : BaseTemporaryBlobName<object>
		{

			[UsedImplicitly, Rank(2)]
			public string JobName;
			[UsedImplicitly, Rank(3)]
			public int BlobSetId;

			public ReducedBlobName(string jobName, TimeSpan duration, int blobSetIt)
				: base(DateTime.Now.Add(duration), MapReduceBlobSet.ReducedPrefix)
			{
				JobName = jobName;
				BlobSetId = blobSetIt;
			}

			public static BlobNamePrefix<ReducedBlobName> GetPrefix(string jobName)
			{
				return GetPrefix(new ReducedBlobName(jobName, 0.Minutes(), 0), 3);
			}

		}

		private class AggregatedBlobName : BaseTemporaryBlobName<object>
		{

			[UsedImplicitly, Rank(2)]
			public string JobName;

			public AggregatedBlobName(string jobName, TimeSpan duration)
				: base(DateTime.Now.Add(duration), MapReduceBlobSet.AggregatedPrefix)
			{
				JobName = jobName;
			}

		}

		private class BlobCounterName : BaseTemporaryBlobName<BlobCounter>
		{

			[UsedImplicitly, Rank(2)]
			public string JobName;

			public BlobCounterName(string jobName, TimeSpan duration)
				: base(DateTime.Now.Add(duration), MapReduceBlobSet.CounterPrefix)
			{
				JobName = jobName;
			}

		}

		#endregion

	}

}
