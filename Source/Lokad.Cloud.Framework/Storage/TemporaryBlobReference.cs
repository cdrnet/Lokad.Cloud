#region Copyright (c) Lokad 2009-2010
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
	/// <typeparam name="T">Type referred by the blob reference.</typeparam>
	/// <seealso cref="TemporaryBlobName"/>
	[Serializable, DataContract]
	public class TemporaryBlobReference<T> : BlobReference<T>
	{
		/// <summary>
		/// Returns the garbage collected container.
		/// </summary>
		public sealed override string ContainerName
		{
			get { return CloudService.TemporaryContainer; }
		}

		[UsedImplicitly, Rank(0), DataMember] public readonly DateTimeOffset Expiration;
		[UsedImplicitly, Rank(1), DataMember] public readonly string Prefix;

		/// <summary>
		/// Explicit constructor.
		/// </summary>
		/// <param name="expiration">
		/// Date that triggers the garbage collection.
		/// </param>
		/// <param name="prefix">
		/// Fixed prefix (typically used to avoid overlaps between temporary blob name
		/// inheritor). If the prefix is <c>null</c>then the 
		/// <see cref="DefaultPrefix"/> get used instead.
		/// </param>
		protected TemporaryBlobReference(DateTimeOffset expiration, string prefix)
		{
			Expiration = expiration;
			Prefix = prefix ?? GetType().FullName;
		}

		/// <summary>
		/// Gets a full reference to a temporary blob.
		/// </summary>
		public static TemporaryBlobReference<T> GetNew(DateTimeOffset expiration)
		{
			return new TemporaryBlobReference<T>(expiration, Guid.NewGuid().ToString("N"));
		}

		/// <summary>
		/// Gets a full reference to a temporary blob.
		/// </summary>
		public static TemporaryBlobReference<T> GetNew(DateTimeOffset expiration, string prefix)
		{
			// hyphen used on purpose, not to interfere with parsing later on.
			return new TemporaryBlobReference<T>(expiration, prefix + "-" + Guid.NewGuid().ToString("N"));
		}
	}
}