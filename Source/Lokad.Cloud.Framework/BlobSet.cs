#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections;
using System.Collections.Generic;

// Notes about delegate serialization can be found at
// http://blogs.microsoft.co.il/blogs/aviwortzel/archive/2008/06/20/how-to-serialize-anonymous-delegates.aspx

namespace Lokad.Cloud.Framework
{
	/// <summary>Item locator for <see cref="BlobSet{T}"/> collection.</summary>
	[Serializable]
	public class BlobId
	{
		// not implemented
	}

	/// <summary>The <c>BlobSet</c> is a blob-based scalable collection that
	/// provides scalable iterators (basically mappers and reducers).</summary>
	/// <typeparam name="T">Type being enumerated.</typeparam>
	/// <remarks>The <see cref="BlobSet{T}"/> should be instanciated through the
	/// <see cref="CloudService.GetBlobSet{T}()"/>. This pattern has been chosen
	/// because the <see cref="BlobSet{T}"/> needs arguments passed to the service
	/// through IoC.</remarks>
	public class BlobSet<T> : IEnumerable<T>
	{
		private readonly string _name;

		/// <summary>Storage identifier for this collection.</summary>
		/// <remarks>This identifier is used as <em>prefix</em> through the blob storage
		/// in order to iterate through the collection.</remarks>
		public string Name
		{
			get { return _name; }
		}

		/// <summary>Constructor that specifies the <see cref="Name"/>.</summary>
		internal BlobSet(string name)
		{
			_name = name;
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
			Func<BlobId, U> mapper, M onCompleted, string mappingQueueName, string messageQueueName)
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
		public T this[BlobId blobId]
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>Adds an item and returns the corresponding blob identifier.</summary>
		public BlobId Add(T item)
		{
			throw new NotImplementedException();
		}

		/// <summary>Removes an item based on its identifier.</summary>
		public void Remove(BlobId blobId)
		{
			throw new NotImplementedException();
		}

		/// <summary>Removes an item (relyies on the hashcode and <c>Equals</c> method).</summary>
		public void Remove(T item)
		{
			throw new NotImplementedException();
		}

		/// <summary>Remove all items from within the collection.</summary>
		public void Clear()
		{
			throw new NotImplementedException();
		}

		/// <summary>Clear the collection and then delete the collection itself.</summary>
		public void Delete()
		{
			throw new NotImplementedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
