#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.ServiceFabric.Runtime;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Management facade for cloud assemblies.
	/// </summary>
	public class CloudAssemblies
	{
		readonly IBlobStorageProvider _blobProvider;
		readonly ILog _log;

		/// <summary>
		/// Initializes a new instance of the <see cref="CloudAssemblies"/> class.
		/// </summary>
		public CloudAssemblies(IBlobStorageProvider blobStorageProvider, ILog log)
		{
			_blobProvider = blobStorageProvider;
			_log = log;
		}

		/// <summary>
		/// Enumerate infos of all configured cloud service assemblies.
		/// </summary>
		public IEnumerable<CloudAssemblyInfo> GetAssemblies()
		{
			var buffer = _blobProvider.GetBlob<byte[]>(
				AssemblyLoader.ContainerName,
				AssemblyLoader.PackageBlobName);

			// do not return anything if no assembly is loaded
			if (!buffer.HasValue)
			{
				yield break;
			}

			using (var zipStream = new ZipInputStream(new MemoryStream(buffer.Value)))
			{
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					var isValid = true;
					var version = new Version();
					var assemblyBytes = new byte[entry.Size];

					try
					{
						zipStream.Read(assemblyBytes, 0, assemblyBytes.Length);
						using (var inspector = new AssemblyInspector(assemblyBytes))
						{
							version = inspector.AssemblyVersion;
						}
					}
					catch (Exception ex)
					{
						isValid = false;
						_log.Error(ex, "Assembly failed to unpack and load.");
					}

					yield return new CloudAssemblyInfo
						{
							AssemblyName = entry.Name,
							DateTime = entry.DateTime,
							Version = version,
							SizeBytes = entry.Size,
							IsValid = isValid
						};
				}
			}
		}

		/// <summary>
		/// Configure a .dll assembly file as the new cloud service assembly.
		/// </summary>
		public void SetAssemblyDll(byte[] data, string fileName)
		{
			using (var tempStream = new MemoryStream())
			{
				using (var zip = new ZipOutputStream(tempStream))
				{
					zip.PutNextEntry(new ZipEntry(fileName));
					zip.Write(data, 0, data.Length);
					zip.CloseEntry();
				}

				SetAssemblyZipContainer(tempStream.ToArray());
			}
		}

		/// <summary>
		/// Configure a zip container with one or more assemblies as the new cloud services.
		/// </summary>
		public void SetAssemblyZipContainer(byte[] data)
		{
			_blobProvider.PutBlob(
				AssemblyLoader.ContainerName,
				AssemblyLoader.PackageBlobName,
				data,
				true);
		}

		/// <summary>
		/// Verify whether the provided zip container is valid.
		/// </summary>
		public bool IsValidZipContainer(byte[] data)
		{
			try
			{
				using (var dataStream = new MemoryStream(data))
				using (var zipStream = new ZipInputStream(dataStream))
				{
					ZipEntry entry;
					while ((entry = zipStream.GetNextEntry()) != null)
					{
						var buffer = new byte[entry.Size];
						zipStream.Read(buffer, 0, buffer.Length);
					}
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
