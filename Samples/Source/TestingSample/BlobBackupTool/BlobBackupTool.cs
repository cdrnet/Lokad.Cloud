#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud;

namespace TestingSample
{
	/// <summary>Allows to backup blobs.</summary>
	public class BlobBackupTool
	{
		IBlobStorageProvider _blobStorage;

		/// <summary>Initializes a new instance of the <see cref="T:BackupTool"/> class.</summary>
		/// <param name="blobStorage">The blob storage provider.</param>
		public BlobBackupTool(IBlobStorageProvider blobStorage)
		{
			_blobStorage = blobStorage;
		}

		/// <summary>Backs up a blob.</summary>
		/// <param name="containerName">The name of the container the source blob is located in.</param>
		/// <param name="blobName">The name of the source blob.</param>
		/// <param name="destinationContainer">The name of the destination container (can be the same as the source).</param>
		/// <remarks>The source blob is copied in the destination container; the name of the destination blob is prepended
		/// with a timestamp: the resulting name is 'YYMMDDHHMMSS-OriginalBlobName'.</remarks>
		/// <returns><c>true</c> if the blob is backed up, <c>false</c> otherwise.</returns>
		public bool BackupBlob(string containerName, string blobName, string destinationContainer)
		{
			var sourceBlob = _blobStorage.GetBlob<object>(containerName, blobName);
			if(sourceBlob == null) return false;

			try
			{
				_blobStorage.List(destinationContainer, "");
			}
			catch
			{
				// Destination container does not exist
				return false;
			}

			_blobStorage.PutBlob(destinationContainer, DateTime.Now.ToString("yyMMddHHmmss") + "-" + blobName, sourceBlob);
			return true;
		}

		/// <summary>Backs up all the blobs in a container.</summary>
		/// <param name="containerName">The name of the source container.</param>
		/// <param name="destinationContainer">The name of the destination container (can be the same as the source)</param>
		/// <remarks>The source blobs are copied in the destination container; the names of the destination blobs are prepended
		/// with a timestamp: the resulting name are 'YYMMDDHHMMSS-OriginalBlobName'.</remarks>
		/// <returns><c>true</c> if the blobs are backed up, <c>false</c> otherwise.</returns>
		public bool BackupAllBlobs(string containerName, string destinationContainer)
		{
			IEnumerable<string> sourceBlobs = null;
			try
			{
				sourceBlobs = _blobStorage.List(containerName, "");
			}
			catch
			{
				return false;
			}

			try
			{
				_blobStorage.List(destinationContainer, "");
			}
			catch
			{
				// Destination container does not exist
				return false;
			}

			foreach(string blobName in sourceBlobs)
			{
				var data = _blobStorage.GetBlob<object>(containerName, blobName);
				_blobStorage.PutBlob(destinationContainer, DateTime.Now.ToString("yyMMddHHmmss") + "-" + blobName, data);
			}

			return true;
		}

	}
}
