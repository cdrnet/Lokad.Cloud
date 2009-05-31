#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System.Collections.Generic;

namespace Lokad.Cloud.Core
{
	/// <summary>Abstraction for the Blob Storage.</summary>
	/// <remarks>
	/// This provider represents a <em>logical</em> blob storage, not the actual
	/// Blob Storage. In particular, this provider deals with overflowing buffers
	/// that need to be split in smaller chuncks to be uploaded.
	/// </remarks>
	public interface IBlobStorageProvider
	{
		/// <summary>Creates a new blob container.</summary>
		/// <returns><c>true</c> if the container was actually created and false if
		/// the container already exists.</returns>
		bool CreateContainer(string containerName);

		/// <summary>Delete a container.</summary>
		/// <remarks>Returns <c>true</c> if the container has been actually deleted.</remarks>
		bool DeleteContainer(string containerName);

		/// <summary>Puts a blob.</summary>
		void PutBlob<T>(string containerName, string blobName, T item);

		/// <summary>Gets a blob.</summary>
		T GetBlob<T>(string containerName, string blobName);

		/// <summary>Deletes a blob.</summary>
		bool DeleteBlob(string containerName, string blobName);

		/// <summary>Iterates over the blobs considering the specified prefix.</summary>
		IEnumerable<string> List(string containerName, string prefix);
	}
}
