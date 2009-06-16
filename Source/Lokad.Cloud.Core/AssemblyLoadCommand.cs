#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Core
{
	public class AssemblyLoadCommand : ICommand
	{
		BlobStorageProvider _provider;
		string _containerName;
		string _blobName;

		public AssemblyLoadCommand(BlobStorageProvider provider, string containerName, string blobName)
		{
			_provider = provider;
			_containerName = containerName;
			_blobName = blobName;
		}

		public void Execute()
		{
			var buffer = _provider.GetBlob<byte[]>(_containerName, _blobName);

			// TODO: need to retrieve DLL from zip archive
		}
	}
}
