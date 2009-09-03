#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
	/// <summary>Base class for strong-typed hierarchical blob names, and
	/// strong typed blob content.</summary>
	/// <typeparam name="T">Type contained in the blob.</typeparam>
	/// <remarks>The type <c>T</c> is handy to strengthen
	/// the <see cref="StorageExtensions"/>.</remarks>
	[Serializable]
	public abstract class BaseTypedBlobName<T> : BaseBlobName
	{
	}
}
