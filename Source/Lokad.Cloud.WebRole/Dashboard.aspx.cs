#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Core;

namespace Lokad.Cloud.Web
{
	public partial class Dashboard : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			// HACK: pseudo-feature, displaying the assemblies contained in the archive.

			var provider = GlobalSetup.Container.Resolve<BlobStorageProvider>();

			var buffer = provider.GetBlob<byte[]>(
				AssemblyLoadCommand.DefaultContainerName, 
				AssemblyLoadCommand.DefaultBlobName);

			using (var zipStream = new ZipInputStream(new MemoryStream(buffer)))
			{
				ZipEntry entry;
				while ((entry = zipStream.GetNextEntry()) != null)
				{
					var data = new byte[entry.Size];
					zipStream.Read(data, 0, (int)entry.Size);

					// TODO: display ZIP content
				}
			}
		}
	}
}
