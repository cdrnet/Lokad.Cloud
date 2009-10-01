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
	/// <seealso cref="MapReduceService"/>
	/// <seealso cref="MapReduceJob"/>
	public sealed class MapReduceBlobSet
	{
		/// <summary>The queue used for managing map/reduce work items (<seealso cref="T:BatchMessage"/>).</summary>
		internal const string JobsQueueName = "blobsets";

		internal const string ContainerName = "blobsets";
		internal const string ConfigPrefix = "config";
		internal const string InputPrefix = "input";
		internal const string ReducedPrefix = "reduced";
		internal const string AggregatedPrefix = "aggregated";
		internal const string CounterPrefix = "counter";

		// Final blob names:
		// - blobsets/config/<job-name> -- map/reduce/aggregate functions plus number of queued blobsets -- readonly
		// - blobsets/input/<job-name>/<blob-guid>
		// - blobsets/reduced/<job-name>/<blob-guid>
		// - blobsets/aggregated/<job-name>
		// - blobsets/counter/<job-name>

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

		MapReduceConfiguration GetJobConfig(string jobName)
		{
			var configBlobName = new MapReduceConfigurationName(jobName);
			var config = _blobStorage.GetBlob<MapReduceConfiguration>(configBlobName);
			return config;
		}

		/// <summary>Generates the blob sets that are required to run cloud-based map/reduce operations.</summary>
		/// <param name="jobName">The name of the job (should be unique).</param>
		/// <param name="items">The items that must be processed (at least two).</param>
		/// <param name="functions">The map/reduce/aggregate functions (aggregate is optional).</param>
		/// <param name="workerCount">The number of workers to use.</param>
		/// <exception cref="ArgumentException">If <paramref name="items"/> contains less than two items.</exception>
		/// <remarks>This method should be called from <see cref="T:MapReduceJob"/>.</remarks>
		public void GenerateBlobSets(string jobName, IList<object> items, MapReduceFunctions functions, int workerCount)
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

			// 1.1. Allocate blobsets
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

			// 1.2. Store input data (separate cycle for clarity)
			processedBlobs = 0;
			for(int currSet = 0; currSet < blobSetCount; currSet++)
			{
				for(int i = 0; i < allNames[currSet].Length; i++)
				{
					// BlobSet and Blob IDs start from zero (see step 3)
					allNames[currSet][i] = new InputBlobName(jobName, currSet, i);

					var item = items[processedBlobs];
					_blobStorage.PutBlob(allNames[currSet][i], item);
					processedBlobs++;
				}
			}

			// 2. Store configuration
			var configBlobName = new MapReduceConfigurationName(jobName);
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
			var config = GetJobConfig(jobName);

			var blobsetPrefix = InputBlobName.GetPrefix(jobName, blobSetId);
			var mapResults = new List<object>();
			
			// 2. Do map for all blobs in the blobset
			foreach(var blobName in _blobStorage.List(blobsetPrefix))
			{
				object inputBlob = _blobStorage.GetBlob<object>(blobName);

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
			var reducedBlobName = new ReducedBlobName(jobName, blobSetId);
			_blobStorage.PutBlob<object>(reducedBlobName, mapResults[0]);

			// 5. Update counter
			var counterName = new BlobCounterName(jobName);
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
			var config = GetJobConfig(jobName);

			var reducedBlobPrefix = ReducedBlobName.GetPrefix(jobName);
			var aggregateResults = new List<object>();

			// 2. Load reduced items and do aggregation
			foreach(var blobName in _blobStorage.List(reducedBlobPrefix))
			{
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
			var aggregatedBlobName = new AggregatedBlobName(jobName);
			_blobStorage.PutBlob(aggregatedBlobName, aggregateResults[0]);

			// 4. Delete reduced data
			foreach(var blobName in _blobStorage.List(reducedBlobPrefix))
			{
				_blobStorage.DeleteBlob(blobName);
			}
		}

		/// <summary>Gets the number of completed blobsets of a job.</summary>
		/// <param name="jobName">The name of the job.</param>
		/// <returns>The number of completed blobsets (<c>Tuple.Item1</c>) and the total number of blobsets (<c>Tuple.Item2</c>).</returns>
		/// <exception cref="ArgumentException">If <paramref name="jobName"/> refers to an inexistent job.</exception>
		public Tuple<int, int> GetCompletedBlobSets(string jobName)
		{
			var config = GetJobConfig(jobName);
			if(config == null) throw new ArgumentException("Unknown job", "jobName");

			var counter = new BlobCounter(_blobStorage, new BlobCounterName(jobName));
			int completedBlobsets = (int)counter.GetValue();

			return new Tuple<int, int>(completedBlobsets, config.BlobSetCount);
		}

		/// <summary>Retrieves the aggregated result of a map/reduce/aggregate job.</summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="jobName">The name of the job.</param>
		/// <returns>The aggregated result.</returns>
		/// <exception cref="InvalidOperationException">If the is not complete yet or if the aggregate function was not specified.</exception>
		/// <exception cref="ArgumentException">If <paramref name="jobName"/> refers to an inexistent job.</exception>
		public T GetAggregatedResult<T>(string jobName)
		{
			var config = GetJobConfig(jobName);
			if(config == null) throw new ArgumentException("Unknown job", "jobName");

			var counter = new BlobCounter(_blobStorage, new BlobCounterName(jobName));
			int completedBlobsets = (int)counter.GetValue();

			if(config.MapReduceFunctions.Aggregator == null) throw new InvalidOperationException("Aggregator function was not specified");
			if(completedBlobsets < config.BlobSetCount) throw new InvalidOperationException("Job is not complete (there still are blobsets to process)");

			var blobName = new AggregatedBlobName(jobName);
			var aggregatedResult = _blobStorage.GetBlob<object>(blobName);

			if(aggregatedResult == null) throw new InvalidOperationException("Job is not complete (reduced items must still be aggregated)");
			else return (T)aggregatedResult;
		}

		/// <summary>Retrieves the reduced results of a map/reduce job (no aggregate function).</summary>
		/// <typeparam name="T">The type of the result.</typeparam>
		/// <param name="jobName">The name of the job.</param>
		/// <returns>The reduced results.</returns>
		/// <exception cref="InvalidOperationException">If the job does not exist or is not complete yet.</exception>
		public IList<T> GetReducedResults<T>(string jobName)
		{
			var config = GetJobConfig(jobName);
			if(config == null) throw new ArgumentException("Unknown job", "jobName");

			var counter = new BlobCounter(_blobStorage, new BlobCounterName(jobName));
			int completedBlobsets = (int)counter.GetValue();

			if(config.MapReduceFunctions.Aggregator != null) throw new InvalidOperationException("Aggregator function was specified");
			if(completedBlobsets < config.BlobSetCount) throw new InvalidOperationException("Job is not complete (there still are blobsets to process)");

			var results = new List<T>();
			foreach(var blobName in _blobStorage.List(ReducedBlobName.GetPrefix(jobName)))
			{
				results.Add(_blobStorage.GetBlob<T>(blobName));
			}

			return results;
		}

		/// <summary>Deletes all the data related to a job, regardless of the job status.</summary>
		/// <param name="jobName">The name of the job.</param>
		/// <remarks>Messages enqueued cannot be deleted but they cause no harm.</remarks>
		public void DeleteJobData(string jobName)
		{
			_blobStorage.DeleteBlob(new MapReduceConfigurationName(jobName));

			foreach(var blobName in _blobStorage.List(InputBlobName.GetPrefix(jobName)))
			{
				_blobStorage.DeleteBlob(blobName);
			}

			foreach(var blobName in _blobStorage.List(ReducedBlobName.GetPrefix(jobName)))
			{
				_blobStorage.DeleteBlob(blobName);
			}

			_blobStorage.DeleteBlob(new AggregatedBlobName(jobName));

			_blobStorage.DeleteBlob(new BlobCounterName(jobName));
		}

		/// <summary>Gets the existing jobs.</summary>
		/// <returns>The names of the existing jobs.</returns>
		public IList<string> GetExistingJobs()
		{
			var names = new List<string>();

			foreach(var blobName in _blobStorage.List(MapReduceConfigurationName.GetPrefix()))
			{
				names.Add(blobName.JobName);
			}

			return names;
		}

		#region Delegate Utils

		/// <summary>Use reflection to invoke a delegate.</summary>
		static object InvokeAsDelegate(object target, params object[] inputs)
		{
			return target.GetType().InvokeMember(
				"Invoke", System.Reflection.BindingFlags.InvokeMethod, null, target, inputs);
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

		public class MapReduceConfigurationName : BaseTypedBlobName<MapReduceConfiguration>
		{
			public override string ContainerName
			{
				get { return MapReduceBlobSet.ContainerName; }
			}

			[UsedImplicitly, Rank(0)]
			public string Prefix;
			[UsedImplicitly, Rank(1)]
			public string JobName;

			public MapReduceConfigurationName(string jobName)
			{
				Prefix = ConfigPrefix;
				JobName = jobName;
			}

			public static BlobNamePrefix<MapReduceConfigurationName> GetPrefix()
			{
				return GetPrefix(new MapReduceConfigurationName(null), 1);
			}

		}

		private class InputBlobName : BaseTypedBlobName<object>
		{
			public override string ContainerName
			{
				get { return MapReduceBlobSet.ContainerName; }
			}

			[UsedImplicitly, Rank(0)]
			public string Prefix;
			[UsedImplicitly, Rank(1)]
			public string JobName;
			[UsedImplicitly, Rank(2)]
			public int BlobSetId;
			[UsedImplicitly, Rank(3)]
			public int BlobId;

			public InputBlobName(string jobName, int blobSetId, int blobId)
			{
				Prefix = InputPrefix;
				JobName = jobName;
				BlobSetId = blobSetId;
				BlobId = blobId;
			}

			public static BlobNamePrefix<InputBlobName> GetPrefix(string jobName, int blobSetId)
			{
				return GetPrefix(new InputBlobName(jobName, blobSetId, 0), 3);
			}

			public static BlobNamePrefix<InputBlobName> GetPrefix(string jobName)
			{
				return GetPrefix(new InputBlobName(jobName, 0, 0), 2);
			}

		}

		private class ReducedBlobName : BaseTypedBlobName<object>
		{
			public override string ContainerName
			{
				get { return MapReduceBlobSet.ContainerName; }
			}

			[UsedImplicitly, Rank(0)]
			public string Prefix;
			[UsedImplicitly, Rank(1)]
			public string JobName;
			[UsedImplicitly, Rank(2)]
			public int BlobSetId;

			public ReducedBlobName(string jobName, int blobSetIt)
			{
				Prefix = ReducedPrefix;
				JobName = jobName;
				BlobSetId = blobSetIt;
			}

			public static BlobNamePrefix<ReducedBlobName> GetPrefix(string jobName)
			{
				return GetPrefix(new ReducedBlobName(jobName, 0), 2);
			}

		}

		private class AggregatedBlobName : BaseTypedBlobName<object>
		{
			public override string ContainerName
			{
				get { return MapReduceBlobSet.ContainerName; }
			}

			[UsedImplicitly, Rank(0)]
			public string Prefix;
			[UsedImplicitly, Rank(1)]
			public string JobName;

			public AggregatedBlobName(string jobName)
			{
				Prefix = AggregatedPrefix;
				JobName = jobName;
			}

		}

		private class BlobCounterName : BaseTypedBlobName<BlobCounter>
		{
			public override string ContainerName
			{
				get { return MapReduceBlobSet.ContainerName; }
			}

			[UsedImplicitly, Rank(0)]
			public string Prefix;
			[UsedImplicitly, Rank(1)]
			public string JobName;

			public BlobCounterName(string jobName)
			{
				Prefix = CounterPrefix;
				JobName = jobName;
			}

		}

		#endregion

	}

}
