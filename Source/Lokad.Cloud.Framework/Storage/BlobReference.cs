#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Storage
{
	/// <summary>
	/// Base class for strongly typed hierarchical references to blobs of a
	/// strongly typed content.
	/// </summary>
	/// <typeparam name="T">Type contained in the blob.</typeparam>
	/// <remarks>
	/// The type <c>T</c> is handy to strengthen the 
	/// <see cref="StorageExtensions"/>.
	/// </remarks>
	[Serializable, DataContract]
	public abstract class BlobReference<T> : BlobName
	{
	}
}