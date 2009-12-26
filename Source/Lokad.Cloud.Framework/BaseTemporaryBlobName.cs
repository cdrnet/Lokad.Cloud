﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Quality;

namespace Lokad.Cloud
{
	/// <summary>Name associated to a strong typed fixed-lifespan item.</summary>
	/// <typeparam name="T">Type referred by the blob name.</typeparam>
	[Serializable, DataContract]
	public class BaseTemporaryBlobName<T> : BaseTypedBlobName<T>
	{
		/// <summary>Returns the garbage collected container.</summary>
		public sealed override string ContainerName
		{
			get { return CloudService.TemporaryContainer; }
		}

		[UsedImplicitly, Rank(0), DataMember] public readonly DateTimeOffset Expiration;
		[UsedImplicitly, Rank(1), DataMember] public readonly string Prefix;

		/// <summary>Explicit constructor.</summary>
		/// <param name="expiration">Date that triggers the garbage collection.</param>
		/// <param name="prefix">Fixed prefix (typically used to avoid overlaps
		/// between temporary blob name inheritor). If the prefix is <c>null</c>
		/// then the <see cref="DefaultPrefix"/> get used instead.</param>
		protected BaseTemporaryBlobName(DateTimeOffset expiration, string prefix)
		{
			Expiration = expiration;
			Prefix = prefix ?? DefaultPrefix;
		}

		/// <summary>Gets a default prefix (actually it's <see cref="Type.FullName"/>).</summary>
		protected string DefaultPrefix
		{
			get { return GetType().FullName; }
		}
	}
}
