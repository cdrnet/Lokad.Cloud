#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Azure;

namespace Lokad.Cloud.Web
{
	public partial class Assemblies : System.Web.UI.Page
	{
		readonly IBlobStorageProvider _provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{
			AssembliesView.DataSource = GetZipEntries();
			AssembliesView.DataBind();
		}

		IEnumerable<object> GetZipEntries()
		{
			var buffer = _provider.GetBlob<byte[]>(
				AssemblyLoader.ContainerName,
				AssemblyLoader.PackageBlobName);

			// do not return anything is no assembly is loaded
			if(null == buffer) yield break;

			using (var zipStream = new ZipInputStream(new MemoryStream(buffer)))
			{
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					byte[] assemblyBytes = new byte[entry.Size];
					zipStream.Read(assemblyBytes, 0, assemblyBytes.Length);

					string version = "n/a";
					using(var inspector = new AssemblyInspector(assemblyBytes))
					{
						version = inspector.AssemblyVersion;
					}

					yield return new
					{
						Name = entry.Name,
						DateTime = entry.DateTime,
						Version = version,
						Size = entry.Size
					};
				}
			}

		}

		protected void UploadButton_Click(object sender, EventArgs e)
		{
			Page.Validate("Upload");
			if(!Page.IsValid) return;

			if(!AssemblyFileUpload.HasFile) // defensive design, the validator should prevent this
			{
				return;
			}

			string extension = GetLowercaseExtension(AssemblyFileUpload.FileName);

			// If the file is a DLL, it must be compressed as ZIP
			if(extension == ".dll")
			{
				using(var tempStream = new MemoryStream())
				{
					using(var zip = new ZipOutputStream(tempStream))
					{
						var entry = new ZipEntry(AssemblyFileUpload.FileName);
						zip.PutNextEntry(entry);

						var bytes = AssemblyFileUpload.FileBytes;
						zip.Write(bytes, 0, bytes.Length);
						zip.CloseEntry();
					}

					_provider.PutBlob(AssemblyLoader.ContainerName,
						AssemblyLoader.PackageBlobName,
						tempStream.ToArray(), true);
				}
			}
			else
			{
				// pushing new archive to storage
				_provider.PutBlob(
					AssemblyLoader.ContainerName,
					AssemblyLoader.PackageBlobName,
					AssemblyFileUpload.FileBytes, true);
			}

			AssembliesView.DataBind();

			UploadSucceededLabel.Visible = true;
		}

		private static string GetLowercaseExtension(string fileName)
		{
			string extension = Path.GetExtension(fileName);
			if(extension == null) return "";

			extension = extension.ToLowerInvariant();
			return extension;
		}

		protected void UploadValidator_Validate(object source, ServerValidateEventArgs args)
		{
			args.IsValid = true;

			// file must exists
			args.IsValid &= AssemblyFileUpload.HasFile;

			// Extension must be ".zip" or ".dll"
			var extension = GetLowercaseExtension(AssemblyFileUpload.FileName);
			args.IsValid &= extension == ".zip" || extension == ".dll";

			if(args.IsValid)
			{
				if(extension == ".zip")
				{
					// checking that the archive can be decompressed correctly.
					try
					{
						using(var zipStream = new ZipInputStream(new MemoryStream(AssemblyFileUpload.FileBytes)))
						{
							ZipEntry entry;
							while((entry = zipStream.GetNextEntry()) != null)
							{
								var buffer = new byte[entry.Size];
								zipStream.Read(buffer, 0, buffer.Length);
							}
						}
					}
					catch(Exception)
					{
						args.IsValid = false;
					}
				}
			}
		}
	}
}
