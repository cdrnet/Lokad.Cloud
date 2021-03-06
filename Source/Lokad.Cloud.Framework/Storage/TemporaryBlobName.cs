﻿#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Services;
using Lokad.Quality;

namespace Lokad.Cloud.Storage
{
	/// <summary>
	/// Reference to a unique blob with a fixed limited lifespan.
	/// </summary>
	/// <remarks>
	/// Used in conjunction with the <see cref="GarbageCollectorService"/>. Use as
	/// base class for custom temporary blobs with additional attributes, or use
	/// the method 
	/// <see cref="GetNew(System.DateTimeOffset)"/> to instantiate a new instance
	/// directly linked to the garbage collected container.
	/// </remarks>
	/// <typeparam name="T">Type referred by the blob name.</typeparam>
	[Serializable, DataContract]
	public class TemporaryBlobName<T> : BlobName<T>
	{
		/// <summary>
		/// Returns the garbage collected container.
		/// </summary>
		public sealed override string ContainerName
		{
			get { return CloudService.TemporaryContainer; }
		}

		[UsedImplicitly, Rank(0), DataMember] public readonly DateTimeOffset Expiration;
		[UsedImplicitly, Rank(1), DataMember] public readonly string Suffix;

		/// <summary>
		/// Explicit constructor.
		/// </summary>
		/// <param name="expiration">
		/// Date that triggers the garbage collection.
		/// </param>
		/// <param name="suffix">
		/// Static suffix (typically used to avoid overlaps between temporary blob name
		/// inheritor). If the provided suffix is <c>null</c>then the 
		/// default prefix <c>GetType().FullName</c> is used instead.
		/// </param>
		protected TemporaryBlobName(DateTimeOffset expiration, string suffix)
		{
			Expiration = expiration;
			Suffix = suffix ?? GetType().FullName;
		}

		/// <summary>
		/// Gets a full name to a temporary blob.
		/// </summary>
		public static TemporaryBlobName<T> GetNew(DateTimeOffset expiration)
		{
			return new TemporaryBlobName<T>(expiration, Guid.NewGuid().ToString("N"));
		}

		/// <summary>
		/// Gets a full name to a temporary blob.
		/// </summary>
		public static TemporaryBlobName<T> GetNew(DateTimeOffset expiration, string prefix)
		{
			// hyphen used on purpose, not to interfere with parsing later on.
			return new TemporaryBlobName<T>(expiration, prefix + "-" + Guid.NewGuid().ToString("N"));
		}
	}
}