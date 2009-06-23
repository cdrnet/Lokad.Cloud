﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip; 

namespace Lokad.Cloud.Core
{
	public class AssemblyLoadCommand : ICommand
	{
		public const string DefaultContainerName = "lokad-cloud-assemblies";

		public const string DefaultBlobName = "default";

		readonly BlobStorageProvider _provider;
		
		public string ContainerName { get; set; }

		public string BlobName { get; set; }

		public AssemblyLoadCommand(BlobStorageProvider provider)
		{
			_provider = provider;
			ContainerName = DefaultContainerName;
			BlobName = DefaultBlobName;
		}

		public void Execute()
		{
			var buffer = _provider.GetBlob<byte[]>(ContainerName, BlobName);

			using(var zipStream = new ZipInputStream(new MemoryStream(buffer)))
			{
				ZipEntry entry;
				while((entry = zipStream.GetNextEntry()) != null)
				{
					var data = new byte[entry.Size];
					zipStream.Read(data, 0, (int)entry.Size);

					// skipping everything but assemblies
					if (!entry.IsFile || !entry.Name.EndsWith(".dll")) continue;

					// loading assembly from data packed in zip
					Assembly.Load(data);
				}
			}
		}
	}
}
