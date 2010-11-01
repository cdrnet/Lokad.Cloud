﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Lokad.Cloud.Storage
{
	/// <summary>Abstraction for the Blob Storage.</summary>
	/// <remarks>
	/// This provider represents a <em>logical</em> blob storage, not the actual
	/// Blob Storage. In particular, this provider deals with overflowing buffers
	/// that need to be split in smaller chunks to be uploaded.
	/// </remarks>
	public interface IBlobStorageProvider
	{
		/// <summary>Creates a new blob container.</summary>
		/// <returns><c>true</c> if the container was actually created and <c>false</c> if
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
		/// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
		/// exists but could not be overwritten.</returns>
		bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite);

		/// <summary>Puts a blob and optionally overwrite.</summary>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobName">Name of the blob.</param>
		/// <param name="item">Item to be put.</param>
		/// <param name="overwrite">Indicates whether existing blob should be overwritten
		/// if it exists.</param>
		/// <param name="etag">New etag (identifier used to track for blob change) if
		/// the blob is written, or <c>null</c> if no blob is written.</param>
		/// <remarks>Creates the container if it does not exist beforehand.</remarks>
		/// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
		/// exists but could not be overwritten.</returns>
		bool PutBlob<T>(string containerName, string blobName, T item, bool overwrite, out string etag);

        /// <summary>Puts a blob only if etag given in argument is matching blob's etag in blobStorage.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="blobName">Name of the blob.</param>
        /// <param name="item">Item to be put.</param>
        /// <param name="expectedEtag">etag that should be matched inside BlobStorage.</param>
        /// <remarks>Creates the container if it does not exist beforehand.</remarks>
        /// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
        /// exists but version were not matching.</returns>
	    bool PutBlob<T>(string containerName, string blobName, T item, string expectedEtag);

		/// <summary>Puts a blob and optionally overwrite.</summary>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobName">Name of the blob.</param>
		/// <param name="item">Item to be put.</param>
		/// <param name="type">The type of the blob.</param>
		/// <param name="overwrite">Indicates whether existing blob should be overwritten
		/// if it exists.</param>
		/// <param name="etag">New etag (identifier used to track for blob change) if
		/// the blob is written, or <c>null</c> if no blob is written.</param>
		/// <remarks>Creates the container if it does not exist beforehand.</remarks>
		/// <returns><c>true</c> if the blob has been put and <c>false</c> if the blob already
		/// exists but could not be overwritten.</returns>
		/// <remarks>This method should only be used when the caller does not know the type of the
		/// object stored in the blob at compile time, but it can only be determined at run time.
		/// In all other cases, you should use the generic overloads of the method.</remarks>
		bool PutBlob(string containerName, string blobName, object item, Type type, bool overwrite, out string etag);

		/// <summary>Gets a blob.</summary>
		/// <returns>
		/// If there is no such blob, the returned object
		/// has its property HasValue set to <c>false</c>.
		/// </returns>
		Maybe<T> GetBlob<T>(string containerName, string blobName);

		/// <summary>Gets a blob.</summary>
		/// <typeparam name="T">Blob type.</typeparam>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobName">Name of the blob.</param>
		/// <param name="etag">Identifier assigned by the storage to the blob
		/// that can be used to distinguish be successive version of the blob 
		/// (useful to check for blob update).</param>
		/// <returns>
		/// If there is no such blob, the returned object
		/// has its property HasValue set to <c>false</c>.
		/// </returns>
		Maybe<T> GetBlob<T>(string containerName, string blobName, out string etag);

		/// <summary>Gets a blob.</summary>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobName">Name of the blob.</param>
		/// <param name="type">The type of the blob.</param>
		/// <param name="etag">Identifier assigned by the storage to the blob
		/// that can be used to distinguish be successive version of the blob 
		/// (useful to check for blob update).</param>
		/// <returns>
		/// If there is no such blob, the returned object
		/// has its property HasValue set to <c>false</c>.
		/// </returns>
		/// <remarks>This method should only be used when the caller does not know the type of the
		/// object stored in the blob at compile time, but it can only be determined at run time.
		/// In all other cases, you should use the generic overloads of the method.</remarks>
		Maybe<object> GetBlob(string containerName, string blobName, Type type, out string etag);

		/// <summary>
		/// Gets a blob in intermediate XML representation for inspection and recovery,
		/// if supported by the serialization formatter.
		/// </summary>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobName">Name of the blob.</param>
		/// <param name="etag">Identifier assigned by the storage to the blob
		/// that can be used to distinguish be successive version of the blob 
		/// (useful to check for blob update).</param>
		/// <returns>
		/// If there is no such blob or the formatter supports no XML representation,
		/// the returned object has its property HasValue set to <c>false</c>.
		/// </returns>
		Maybe<XElement> GetBlobXml(string containerName, string blobName, out string etag);

		/// <summary>
		/// Gets a range of blobs.
		/// </summary>
		/// <typeparam name="T">Blob type.</typeparam>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobNames">Names of the blobs.</param>
		/// <param name="etags">Etag identifiers for all returned blobs.</param>
		/// <returns>For each requested blob, an element in the array is returned in the same order.
		/// If a specific blob was not found, the corresponding <b>etags</b> array element is <c>null</c>.</returns>
		Maybe<T>[] GetBlobRange<T>(string containerName, string[] blobNames, out string[] etags);

		/// <summary>Gets a blob only if the etag has changed meantime.</summary>
		/// <typeparam name="T">Type of the blob.</typeparam>
		/// <param name="containerName">Name of the container.</param>
		/// <param name="blobName">Name of the blob.</param>
		/// <param name="oldEtag">Old etag value. If this value is <c>null</c>, the blob will always
		/// be retrieved (except if the blob does not exist anymore).</param>
		/// <param name="newEtag">New etag value. Will be <c>null</c> if the blob no more exist,
		/// otherwise will be set to the current etag value of the blob.</param>
		/// <returns>
		/// If the blob has not been modified or if there is no such blob,
		/// then the returned object has its property HasValue set to <c>false</c>.
		/// </returns>
		Maybe<T> GetBlobIfModified<T>(string containerName, string blobName, string oldEtag, out string newEtag);

		/// <summary>
		/// Gets the current etag of the blob, or <c>null</c> if the blob does not exists.
		/// </summary>
		string GetBlobEtag(string containerName, string blobName);

		/// <summary>Update a blob while guaranteeing an atomic update process.</summary>
		/// <param name="containerName"></param>
		/// <param name="blobName"></param>
		/// <param name="updater">The function takes a <c>T</c> object to update
		/// and returns a <see cref="Result{T}"/> if update has succeed,
		/// because the updater can optionally decide not to succeed with the update
		/// (in case where the update no more relevant for example.</param>
		/// <param name="result">Result returned by the updated.</param>
		/// <returns><c>true</c> if the update is successful.
		/// If the blob is updated between the retrieval and the update attempt,
		/// then no update is performed and the method returns <c>false</c>.</returns>
		/// <remarks>If there is not such blob available, the update is performed with
		/// the default <c>T</c> value.</remarks>
		bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, Result<T>> updater, out Result<T> result);

		/// <seealso cref="UpdateIfNotModified{T}(string,string,System.Func{T,Lokad.Result{T}},out Lokad.Result{T})"/>
		bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, T> updater, out T result);

		/// <summary>Update a blob while guaranteeing an atomic update process.</summary>
		bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, Result<T>> updater);

		/// <seealso cref="UpdateIfNotModified{T}(string,string,System.Func{T,Lokad.Result{T}})"/>
		bool UpdateIfNotModified<T>(string containerName, string blobName, Func<Maybe<T>, T> updater);

		/// <summary>Deletes a blob.</summary>
		bool DeleteBlob(string containerName, string blobName);

		/// <summary>Iterates over the blobs considering the specified prefix.</summary>
		IEnumerable<string> List(string containerName, string prefix);
	}
}