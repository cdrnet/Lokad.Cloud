#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Lokad.Cloud.Services;

// TODO: [vermorel] The algorithm that enables fast iteration is subtle (and not implemented yet)
// and will be provided as a white paper along with the Lokad.Cloud documentation.

namespace Lokad.Cloud.Framework
{
	/// <summary>Item locator for <see cref="BlobSet{T}"/> collection.</summary>
	[Serializable]
	public class BlobLocator
	{
		readonly string _name;

		public string Name
		{
			get { return _name; }
		}

		public BlobLocator(string name)
		{
			_name = name;
		}
	}

	/// <summary>Settings of a map operation.</summary>
	[Serializable]
	public class BlobSetMapSettings
	{
		/// <summary>Underlying type is expected to be <see cref="Func{T,TResult}"/>.</summary>
		public object Mapper { get; set; }

		public object OnCompleted { get; set; }
		public string OnCompletedQueueName { get; set; }
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
		static Random _rand = new Random();
		static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();
		const int HexDepth = 8;

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
			var completionQueueName = _providers.TypeMapper.GetStorageName(typeof(M));
			MapToBlobSet(destPrefix, mapper, onCompleted, completionQueueName);
		}

		/// <summary>Apply the specified mapping  to all items of this collection.</summary>
		/// <param name="destPrefix">Prefix to be used for the destination <c>BlobSet</c>.</param>
		/// <typeparam name="U">Output type of the mapped items.</typeparam>
		/// <typeparam name="M">Output type of the termination message.</typeparam>
		/// <param name="mapper">Mapping function (should be serializable).</param>
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

			_providers.BlobStorage.PutBlob(
				ContainerName, destPrefix + Delimiter + MapSettingsSuffix, settings);

			var counterBlobName = destPrefix + Delimiter + MapCounterSuffix;
			var counter = new BlobCounter(_providers, ContainerName, counterBlobName);
			counter.Reset(BlobCounter.Aleph);

			var itemCount = 0l;
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
			if(0l == res)
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
			ReduceToQueue(reducer, _providers.TypeMapper.GetStorageName(typeof(U)));
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

			var itemCount = 0l;
			foreach (var blobName in _providers.BlobStorage.List(ContainerName, _prefix))
			{
				// listing directly the wrappers (to avoid retrieving items).
				var wrapper = new MessageWrapper { ContainerName = ContainerName, BlobName = blobName };
				_providers.QueueStorage.Put(workQueue, wrapper);
				itemCount++;
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

		/// <summary>Retrieves an item based on the blob identifier.</summary>
		public T this[BlobLocator locator]
		{
			get
			{
				return _providers.BlobStorage.GetBlob<T>(ContainerName, locator.Name);
			}
		}

		/// <summary>Adds an item and returns the corresponding blob identifier.</summary>
		public BlobLocator Add(T item)
		{
			var blobName = GetNewItemBlobName();
			_providers.BlobStorage.PutBlob(ContainerName, blobName, item);
			return new BlobLocator(blobName);
		}

		/// <summary>Removes an item based on its identifier.</summary>
		/// <returns><c>true</c> if the blob was successfully removed and <c>false</c> otherwise.</returns>
		public bool Remove(BlobLocator locator)
		{
			return _providers.BlobStorage.DeleteBlob(ContainerName, locator.Name);
		}

		/// <summary>Remove all items from within the collection. Method is asynchronous,
		/// it returns immediately once the deletion task is queued.</summary>
		/// <remarks>Considering that the <see cref="BlobSet{T}"/> is nothing
		/// but a list of prefixed blobs in a container of the Blob Storage,
		/// clearing the collection is equivalent to deleting the collection.
		/// </remarks>
		public void Clear()
		{
			// TODO: BlobSet.Clear must be implemented
			throw new NotImplementedException();
		}

		/// <summary>Get a new blob name including the prefix, the pseudo-random pattern plus
		/// the Guid. Those names are choosen to avoid collision and facilitate fast iteration.
		/// </summary>
		string GetNewItemBlobName()
		{
			var builder = new StringBuilder();
			builder.Append(_prefix);
			builder.Append(Delimiter);
            
			// Required for fast iteration
			for(int i = 0; i < HexDepth; i++)
			{
				builder.Append(HexDigits[_rand.Next(16)]);
				builder.Append(Delimiter);
			}

			builder.Append(Guid.NewGuid().ToString());

			return builder.ToString();
		}

		string GetTmpQueueName()
		{
			// Prefix with 'lokad-tmp'
			// Followed by date (so that garbage collection can occur later in case of failure)

			var builder = new StringBuilder();
			builder.Append("lokad-tmp-");
			builder.Append(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd"));
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
			builder.Append(DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd"));
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
