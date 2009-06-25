#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
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

		/// <summary>Puts a blob (overwrite if the blob already exists).</summary>
		/// <remarks>Creates the container if it does not exist beforehand.</remarks>
		void PutBlob<T>(string containerName, string blobName, T item);

		/// <summary>Puts a blob and optionally overwrite.</summary>
		/// <remarks>Creates the container if it does not exist beforehand.</remarks>
		/// <returns><c>true</c> if the blob has been put and false if the blob already
		/// exists but could not be overwritten.</returns>
		bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite);

		/// <summary>Gets a blob.</summary>
		/// <returns>If there is no such blob, a <c>null</c> (or a default value) is
		/// returned.</returns>
		T GetBlob<T>(string containerName, string blobName);

		/// <summary>Update a blob while garantying an atomic update process.</summary>
		/// <returns><c>true</c> if the update is successful.
		/// If the blob is updated between the retrieval and the update attempt,
		/// then no update is performed and the method returns <c>false</c>.</returns>
		/// <remarks>If there is not such blob available, the update is performed with
		/// the default <c>T</c> value.</remarks>
		bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater, out T result);

		/// <summary>Update a blob while garantying an atomic update process.</summary>
		bool UpdateIfNotModified<T>(string containerName, string blobName, Func<T, T> updater);

		/// <summary>Deletes a blob.</summary>
		bool DeleteBlob(string containerName, string blobName);

		/// <summary>Iterates over the blobs considering the specified prefix.</summary>
		IEnumerable<string> List(string containerName, string prefix);
	}
}
