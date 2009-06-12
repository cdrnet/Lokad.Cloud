#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Text;

// Notes about delegate serialization can be found at
// http://blogs.microsoft.co.il/blogs/aviwortzel/archive/2008/06/20/how-to-serialize-anonymous-delegates.aspx

// TODO: [vermorel] The algorithm that enables fast iteration is subtle
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
		/// <typeparam name="U">Output type of the mapped items.</typeparam>
		/// <typeparam name="M">Output type of the termination message.</typeparam>
		/// <param name="mapper">Mapping function (should be serializable).</param>
		/// <param name="onCompleted">Message pushed when the mapping is completed.</param>
		/// <remarks>
		/// This method is asynchronous, all mapped items with will be implicitely
		/// put to the queue matching the type <c>U</c>.
		/// </remarks>
		public void MapToQueue<U, M>(Func<T, U> mapper, M onCompleted)
		{
			throw new NotImplementedException();
		}

		/// <summary>Apply the specified mapping  to all items of this collection.</summary>
		/// <typeparam name="U">Output type of the mapped items.</typeparam>
		/// <typeparam name="M">Output type of the termination message.</typeparam>
		/// <param name="mapper">Mapping function (should be serializable).</param>
		/// <param name="mappingQueueName">Identifier of the queue where items will be put.</param>
		/// <param name="messageQueueName">Identifier of the queue where the termination message is put.</param>
		/// <remarks>
		/// This method is asynchronous.
		/// </remarks>
		public void MapToQueue<U, M>(
			Func<T, U> mapper, M onCompleted, string mappingQueueName, string messageQueueName)
		{
			throw new NotImplementedException();
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
			var blobName = GetNewBlobName();
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
		string GetNewBlobName()
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
	}
}
