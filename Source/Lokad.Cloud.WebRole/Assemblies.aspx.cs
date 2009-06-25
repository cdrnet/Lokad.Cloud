#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Core;

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
				AssemblyLoadCommand.DefaultContainerName,
				AssemblyLoadCommand.DefaultBlobName);

			using (var zipStream = new ZipInputStream(new MemoryStream(buffer)))
			{
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					yield return new
						{
							entry.Name, 
							entry.DateTime, 
							entry.Size
					};
				}
			}
		}

		protected void UploadButton_Click(object sender, EventArgs e)
		{
			if(!AssemblyFileUpload.HasFile) // defensive design, the validator should prevent this
			{
				return;
			}

			// pushing new archive to storage
			_provider.PutBlob(
				AssemblyLoadCommand.DefaultContainerName,
				AssemblyLoadCommand.DefaultBlobName, 
				AssemblyFileUpload.FileBytes, true);

			AssembliesView.DataBind();

			UploadSucceededLabel.Visible = true;
		}

		protected void UploadValidator_Validate(object source, ServerValidateEventArgs args)
		{
			// file must exists
			args.IsValid &= AssemblyFileUpload.HasFile;

			// checking that the archive can be decompressed correctly.
			try
			{
				using (var zipStream = new ZipInputStream(new MemoryStream(AssemblyFileUpload.FileBytes)))
				{
					ZipEntry entry;
					while ((entry = zipStream.GetNextEntry()) != null)
					{
						var buffer = new byte[entry.Size]; 
						zipStream.Read(buffer, 0, buffer.Length);
					}
				}
			}
			catch (Exception)
			{
				args.IsValid = false;
			}
		}
	}
}
