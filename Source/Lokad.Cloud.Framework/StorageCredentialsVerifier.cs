#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud
{
	
	/// <summary>
	/// Verifies that storage credentials are correct and allow access to blob and queue storage.
	/// </summary>
	public class StorageCredentialsVerifier
	{

		private CloudBlobClient _storage;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StorageCredentialsVerifier" /> class.
		/// </summary>
		/// <param name="storage">The blob storage.</param>
		public StorageCredentialsVerifier(CloudBlobClient storage)
		{
			if(storage == null) throw new ArgumentNullException("storage");

			_storage = storage;
		}

		/// <summary>
		/// Verifies the storage credentials.
		/// </summary>
		/// <returns><c>true</c> if the credentials are correct, <c>false</c> otherwise.</returns>
		public bool VerifyCredentials()
		{
			try
			{
				var containers = _storage.ListContainers();

				// It is necssary to enumerate in order to actually send the request
				foreach(var c in containers)
				{
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

	}

}
