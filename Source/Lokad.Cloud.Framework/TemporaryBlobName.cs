#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using Lokad.Cloud.Framework.Services;
using Lokad.Quality;

namespace Lokad.Cloud.Framework
{
	/// <summary>Used in conjunction with the <see cref="GarbageCollectorService"/>.</summary>
	/// <remarks>Use the method <see cref="GetNew"/> to instantiate a new instance
	/// direcly linked to the garbage collected container.</remarks>
	[Serializable]
	public class TemporaryBlobName : BaseBlobName
	{
		public override string ContainerName
		{
			get { return CloudService.TemporaryContainer; }
		}

		// caution: field order DOES matter here.

		[UsedImplicitly]
		public readonly DateTime Expiration;

		[UsedImplicitly]
		public readonly string Suffix;

		public TemporaryBlobName(DateTime expiration, string suffix)
		{
			Expiration = expiration;
			Suffix = suffix;
		}

		/// <summary>Gets a full name for a temporary blob.</summary>
		public static TemporaryBlobName GetNew(DateTime expiration)
		{
			return new TemporaryBlobName(expiration, Guid.NewGuid().ToString("N"));
		}

		/// <summary>Gets a full name for a temporary blob.</summary>
		public static TemporaryBlobName GetNew(DateTime expiration, string prefix)
		{
			// hyphen used on purpose, not to interfere with parsing later on.
			return new TemporaryBlobName(expiration, prefix + "-" + Guid.NewGuid().ToString("N"));
		}
	}
}
