#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Cloud.Services;
using Lokad.Quality;

namespace Lokad.Cloud
{
	/// <summary>
	/// Name of a unique blob with a fixed limited lifespan.
	/// </summary>
	/// <remarks>
	/// Used in conjunction with the <see cref="GarbageCollectorService"/>. Use the
	/// method <see cref="GetNew(System.DateTimeOffset)"/> to instantiate a new
	/// instance directly linked to the garbage collected container.
	/// </remarks>
	/// <seealso cref="TemporaryBlobReference{T}"/>
	[Serializable, DataContract]
	public class TemporaryBlobName : BlobName
	{
		/// <summary>
		/// Returns the garbage collected container.
		/// </summary>
		public override string ContainerName
		{
			get { return CloudService.TemporaryContainer; }
		}

		[UsedImplicitly, Rank(0), DataMember] public readonly DateTimeOffset Expiration;
		[UsedImplicitly, Rank(1), DataMember] public readonly string Suffix;

		TemporaryBlobName(DateTimeOffset expiration, string suffix)
		{
			Expiration = expiration;
			Suffix = suffix;
		}

		/// <summary>
		/// Gets a full name for a temporary blob.
		/// </summary>
		public static TemporaryBlobName GetNew(DateTimeOffset expiration)
		{
			return new TemporaryBlobName(expiration, Guid.NewGuid().ToString("N"));
		}

		/// <summary>
		/// Gets a full name for a temporary blob.
		/// </summary>
		public static TemporaryBlobName GetNew(DateTimeOffset expiration, string prefix)
		{
			// hyphen used on purpose, not to interfere with parsing later on.
			return new TemporaryBlobName(expiration, prefix + "-" + Guid.NewGuid().ToString("N"));
		}
	}
}
