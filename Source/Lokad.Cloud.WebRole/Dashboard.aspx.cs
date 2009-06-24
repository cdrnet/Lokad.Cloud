#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Core;

namespace Lokad.Cloud.Web
{
	public partial class Dashboard : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			ArchiveView.DataSource = GetZipEntries();
			ArchiveView.DataBind();
		}

		static IEnumerable<object> GetZipEntries()
		{
			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

			var buffer = provider.GetBlob<byte[]>(
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
	}
}
