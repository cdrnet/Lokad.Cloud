#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Reflection;
using System.Text;
using Lokad.Cloud;
using Lokad.Quality;

// BUG: replace the call to MessageWrapper by a proper message (see below).

// TODO: #46 need to cache mapper, but there are several constrainsts
// - mapper should be written to unique (and temporary) location using TemporaryBlobName
//	--> if the cloud app gets shut down, there will be no left over later on.
//	--> storage is unique, blob name can be used as cache key to figure out wether mapper is already in cache.
// - mapper is put in cache, should be invalidated after a while. I suggest a duration of 30min or so.
// - the real challenge, at some point, will be to manage some worker affinity
//	--> considering the case where several distinct blobset mapping are taking place.

// TODO: logic of 'GetTmpQueueName' is incorrect, must use the garbage collected container
// TODO: logic of 'GetTmpBlobName' is incorrect, must use the garbage collected container

namespace MapReduce
{
	/// <summary>Settings of a map operation.</summary>
	[Serializable]
	public class BlobSetMapSettings
	{
		/// <summary>Underlying type is expected to be <see cref="Func{T,TResult}"/>.</summary>
		public object Mapper { get; set; }

		public object OnCompleted { get; set; }
		public string OnCompletedQueueName { get; set; }
	}

	class BlobSetMapName : BaseBlobName
	{
		public override string ContainerName
		{
			get { return BlobSet<object>.ContainerName; }
		}

		[UsedImplicitly, Rank(0)] public readonly string Prefix;
		[UsedImplicitly, Rank(1)] public readonly string Suffix;

		public BlobSetMapName(string prefix, string suffix)
		{
			Prefix = prefix;
			Suffix = suffix;
		}
	}

	/// <summary>Settings of a reduce operation.</summary>
	[Serializable]
	public class BlobSetReduceSettings
	{
		/// <summary>Underlying type is expected to be <see cref="Func{T,T,T}"/>.</summary>
		public object Reducer { get; set; }

		/// <summary>Name of the queue dedicated to reduction process.</summary>
		public string WorkQueue { get; set; }

		/// <summary>Name of the queue where the final reduction should be put.</summary>
		public string ReductionQueue { get; set; }

		/// <summary>Suffix of the blob that contains the reduction counter.</summary>
		public string ReductionCounter { get; set; }
	}

	/// <summary>The <c>BlobSet</c> is a blob-based scalable collection that
	/// provides scalable iterators (basically mappers and reducers).</summary>
	/// <typeparam name="T">Type being enumerated.</typeparam>
	/// <remarks>
	/// <para>The <see cref="BlobSet{T}"/> should be instanciated through the
	/// <see cref="CloudService.GetBlobSet{T}()"/>. This pattern has been chosen
	/// because the <see cref="BlobSet{T}"/> needs arguments passed to the service
	/// through IoC.
	/// </para>
	/// <para>All <see cref="BlobSet{T}"/>s are stored in a single blob containers.
	/// They are separated through the usage of a blob name prefix.
	/// </para>
	/// <para>
	/// Items put in a <see cref="BlobSet{T}"/> are giving pseudo-random names.
	/// The pseudo-random pattern is used for fast iteration.
	/// </para>
	/// </remarks>
	public class BlobSet<T>
	{
		/// <summary>Name of the container for all the blobsets.</summary>
		public const string ContainerName = "lokad-blobsets";

		/// <summary>Delimiter used for prefixing iterations on Blob Storage.</summary>
		public const string Delimiter = "/";

		/// <summary>Blob name used to store the mapping during a map operation.</summary>
		public const string MapSettingsSuffix = "map-settings";

		/// <summary>Blob name used to store the number of remaining mappings during
		/// a map operation.</summary>
		public const string MapCounterSuffix = "map-counter";

		readonly ProvidersForCloudStorage _providers;
		readonly string _prefix;

		/// <summary>Storage prefix for this collection.</summary>
		/// <remarks>This identifier is used as <em>prefix</em> through the blob storage
		/// in order to iterate through the collection.</remarks>
		public string Prefix
		{
			get { return _prefix; }
		}

		/// <summary>Constructor that specifies the <see cref="Prefix"/>.</summary>
		/// <remarks>The container name is based on the type <c>T</c>.</remarks>
		internal BlobSet(ProvidersForCloudStorage providers, string prefix)
		{
			_providers = providers;
			_prefix = prefix;
		}

		/// <summary>Apply the specified mapping to all items of this collection.</summary>
		/// <param name="destPrefix">Prefix to be used for the destination <c>BlobSet</c>.</param>
		/// <typeparam name="U">Output type of the mapped items.</typeparam>
		/// <typeparam name="M">Output type of the termination message.</typeparam>
		/// <param name="mapper">Mapping function (should be serializable).</param>
		/// <param name="onCompleted">Message pushed when the mapping is completed.</param>
		/// <remarks>This method is asynchronous.</remarks>
		public void MapToBlobSet<U, M>(string destPrefix, Func<T, U> mapper, M onCompleted)
		{
			var completionQueueName = TypeMapper.GetStorageName(typeof(M));
			MapToBlobSet(destPrefix, mapper, onCompleted, completionQueueName);
		}

		/// <summary>Apply the specified mapping  to all items of this collection.</summary>
		/// <param name="destPrefix">Prefix to be used for the destination <c>BlobSet</c>.</param>
		/// <typeparam name="U">Output type of the mapped items.</typeparam>
		/// <typeparam name="M">Output type of the termination message.</typeparam>
		/// <param name="mapper">Mapping function (should be serializable).</param>
		/// <param name="onCompleted">Termination message.</param>
		/// <param name="onCompletedQueueName">Identifier of the queue where the termination message is put.</param>
		/// <remarks>
		/// This method is asynchronous.
		/// </remarks>
		public void MapToBlobSet<U, M>(
			string destPrefix, Func<T, U> mapper, M onCompleted, string onCompletedQueueName)
		{
			if(Prefix.Equals(destPrefix))
			{
				throw new ArgumentException("Can't map BlobSet to itself, a different destination is needed.");
			}

			// HACK: sequential iteration over the BlobSet (not scalable)

			var settings = new BlobSetMapSettings
				{
					Mapper = mapper,
					OnCompleted = onCompleted,
					OnCompletedQueueName = onCompletedQueueName
				};

			var blobStorage = _providers.BlobStorage; // short-hand

			var settingName = new BlobSetMapName(destPrefix, MapSettingsSuffix);
			blobStorage.PutBlob(settingName, settings);

			var counterBlobName = new BlobSetMapName(destPrefix, MapCounterSuffix);
			var counter = new BlobCounter(blobStorage, counterBlobName);

			counter.Reset(BlobCounter.Aleph);

			var itemCount = 0L;
			foreach (var blobName in _providers.BlobStorage.List(ContainerName, _prefix))
			{
				var message = new BlobSetMapMessage
					{
						ItemSuffix = blobName.Substring(_prefix.Length + 1),
						DestinationPrefix = destPrefix,
						SourcePrefix = _prefix
					};

				_providers.QueueStorage.Put(BlobSetMapService.QueueName, message);
				itemCount++;
			}

			var res = (long)counter.Increment(itemCount - BlobCounter.Aleph);

			// rare race condition  (but possible in theory)
			if(0L == res)
			{
				counter.Delete();

				// pushing the message as a completion signal
				_providers.QueueStorage.Put(onCompletedQueueName, onCompleted);
			}
		}

		/// <summary>Apply a reducing function and outputs to the queue
		/// implicitely selected based on the type <c>U</c>.</summary>
		/// <typeparam name="U">Reduction type.</typeparam>
		/// <param name="reducer">Reducing function.</param>
		public void ReduceToQueue<U>(Func<U, U, U> reducer)
		{
			ReduceToQueue(reducer, TypeMapper.GetStorageName(typeof(U)));
		}

		/// <summary>Apply a reducing function and outputs to the queue specified.</summary>
		/// <typeparam name="U">Reduction type.</typeparam>
		/// <param name="reducer">Reduction type.</param>
		/// <param name="queueName">Identifier the output queue.</param>
		public void ReduceToQueue<U>(Func<U,U,U> reducer, string queueName)
		{
			// HACK: sequential iteration over the blobset (not scalable)
			var workQueue = GetTmpQueueName();

			var settingsBlobName = GetTmpBlobName();
			var counterBlobName = GetTmpBlobName();

			var settings = new BlobSetReduceSettings
				{
					Reducer = reducer,
					ReductionQueue = queueName,
					ReductionCounter = counterBlobName,
					WorkQueue = workQueue
				};

			_providers.BlobStorage.PutBlob(ContainerName, settingsBlobName, settings);

			var counter = new BlobCounter(_providers, ContainerName, counterBlobName);
			counter.Reset(BlobCounter.Aleph);

			var itemCount = 0L;
			foreach (var blobName in _providers.BlobStorage.List(ContainerName, _prefix))
			{
				// listing directly the wrappers (to avoid retrieving items).
				// BUG: replace the call to MessageWrapper by a proper message here.
				//var wrapper = new MessageWrapper { ContainerName = ContainerName, BlobName = blobName };
				//_providers.QueueStorage.Put(workQueue, wrapper);
				itemCount++;

				throw new NotImplementedException();
			}

			var message = new BlobSetReduceMessage
			{
				SourcePrefix = _prefix,
				SettingsSuffix = settingsBlobName
			};

			// HACK: naive parallelization here using at most SQRT(N) workers
			for (int i = 0; i < Math.Sqrt(itemCount); i++)
			{
				_providers.QueueStorage.Put(BlobSetReduceService.QueueName, message);
			}

			// -1 because there are only N-1 reductions for N items.
			counter.Increment(itemCount - 1 - BlobCounter.Aleph);
		}


		string GetTmpQueueName()
		{
			// Prefix with 'lokad-tmp'
			// Followed by date (so that garbage collection can occur later in case of failure)

			var builder = new StringBuilder();
			builder.Append("lokad-tmp-");
			builder.Append(DateTime.UtcNow.ToString("yyyy-MM-dd"));
			builder.Append("-");
			builder.Append(Guid.NewGuid().ToString("N"));

			return builder.ToString();
		}

		string GetTmpBlobName()
		{
			// Prefix with 'lokad-tmp'
			// Followed by date (so that garbage collection can occur later in case of failure)

			var builder = new StringBuilder();
			builder.Append("lokad-tmp");
			builder.Append(Delimiter);
			builder.Append(DateTime.UtcNow.ToString("yyyy-MM-dd"));
			builder.Append(Delimiter);
			builder.Append(Guid.NewGuid().ToString("N"));

			return builder.ToString();
		}

		/// <summary>Use reflection to invoke a delegate.</summary>
		public static object InvokeAsDelegate(object target, params object[] inputs)
		{
			return target.GetType().InvokeMember(
				"Invoke", BindingFlags.InvokeMethod, null, target, inputs);
		}
	}
}
