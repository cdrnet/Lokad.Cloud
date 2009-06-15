#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Text;
using System.Threading;
using Lokad.Cloud.Services;

// Notes about delegate serialization can be found at
// http://blogs.microsoft.co.il/blogs/aviwortzel/archive/2008/06/20/how-to-serialize-anonymous-delegates.aspx

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
		public object Mapper { get; set; }
		public object OnCompleted { get; set; }
		public string OnCompletedQueueName { get; set; }
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
		public const string MapSettingsBlobName = "mapping";

		/// <summary>Blob name used to store the number of remaining mappings during
		/// a map operation.</summary>
		public const string MapCounterBlobName = "counter";

		const long MapInitialShift = 2l ^ 48;

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
			// HACK: sequential iteration over the BlobSet (not scalable)

			var settings = new BlobSetMapSettings
			               	{
			               		Mapper = mapper,
			               		OnCompleted = onCompleted,
			               		OnCompletedQueueName = onCompletedQueueName
			               	};

			_providers.BlobStorage.PutBlob(
				ContainerName, destPrefix + Delimiter + MapSettingsBlobName, settings);

			long ignored;
			var isModified = _providers.BlobStorage.UpdateIfNotModified(
				ContainerName, 
				destPrefix + Delimiter + MapCounterBlobName, 
				x => x + MapInitialShift, 
				out ignored);

			if(!isModified) throw new InvalidOperationException(
				"MapToBlobSet can't be executed twice on the same destination BlobSet.");

			var itemCount = 0l;
			foreach (var blobName in _providers.BlobStorage.List(ContainerName, _prefix))
			{
				var message = new BlobSetMessage
				              	{
				              		ItemSuffix = blobName.Substring(_prefix.Length + 1), 
									DestinationPrefix = destPrefix, 
									SourcePrefix = _prefix
				              	};

				_providers.QueueStorage.Put(BlobSetService.QueueName, new[]{message});
				itemCount++;
			}

			RetryUpdate(() => _providers.BlobStorage.UpdateIfNotModified(
				ContainerName, 
				destPrefix + Delimiter + MapCounterBlobName, 
				x => x + itemCount - MapInitialShift, 
				out ignored));
		}

		/// <summary>Apply a reducing function and outputs to the queue
		/// implicitely selected based on the type <c>U</c>.</summary>
		/// <typeparam name="U">Reduction type.</typeparam>
		/// <param name="reducer">Reducing function.</param>
		public void ReduceToQueue<U>(Func<U, U, U> reducer)
		{
			throw  new NotImplementedException();
		}

		/// <summary>Apply a reducing function and outputs to the queue specified.</summary>
		/// <typeparam name="U">Reduction type.</typeparam>
		/// <param name="reducer">Reduction type.</param>
		/// <param name="queueName">Identifier the output queue.</param>
		public void ReduceToQueue<U>(Func<U,U,U> reducer, string queueName)
		{
			throw new NotImplementedException();
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

		/// <summary>Remove all items from within the collection.</summary>
		/// <remarks>Considering that the <see cref="BlobSet{T}"/> is nothing
		/// but a list of prefixed blobs in a container of the Blob Storage,
		/// clearing the collection is equivalent to deleting the collection.
		/// </remarks>
		public void Clear()
		{
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

		/// <summary>Retry an update method until it succeeds. Timing
		/// increases to avoid overstressing the storage for nothing.</summary>
		/// <param name="func"></param>
		public static void RetryUpdate(Func<bool> func)
		{
			// HACK: hard-code constants, the whole counter system have to be perfected.
			const int InitMaxSleepInMs = 50;
			const int MaxSleepInMs = 2000;

			var maxSleepInMs = InitMaxSleepInMs;

			while(!func())
			{
				var sleepTime = _rand.Next(maxSleepInMs).Milliseconds();
				Thread.Sleep(sleepTime);

				maxSleepInMs += 50;
				maxSleepInMs = Math.Min(maxSleepInMs, MaxSleepInMs);
			}
		}
	}
}
