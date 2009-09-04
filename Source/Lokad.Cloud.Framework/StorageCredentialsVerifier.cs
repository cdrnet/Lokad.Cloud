#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud
{
	
	/// <summary>
	/// Verifies that storage credentials are correct and allow access to blob and queue storage.
	/// </summary>
	public class StorageCredentialsVerifier
	{

		private BlobStorage _storage;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StorageCredentialsVerifier" /> class.
		/// </summary>
		/// <param name="storage">The blob storage.</param>
		public StorageCredentialsVerifier(BlobStorage storage)
		{
			if(storage == null) throw new ArgumentNullException("storage");

			_storage = storage;
		}

		/// <summary>
		/// Verifies the storage credentials.
		/// </summary>
		/// <returns><b>true</b> if the credentials are correct, <b>false</b> otherwise.</returns>
		public bool VerifyCredentials()
		{
			try
			{
				var containers = _storage.ListBlobContainers();

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
