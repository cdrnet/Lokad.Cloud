#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Text;
using System.Web.UI;
using Lokad.Cloud.Azure;

namespace Lokad.Cloud.Web
{
	public partial class Config : Page
	{
		readonly IBlobStorageProvider _blobStorage =
			GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		readonly UTF8Encoding _encoding = new UTF8Encoding();

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				var buffer = _blobStorage.GetBlob<byte[]>(
					AssemblyLoader.ContainerName, AssemblyLoader.ConfigurationBlobName);

				if (buffer.HasValue)
				{
					ConfigurationBox.Text = _encoding.GetString(buffer.Value);
				}
			}
		}

		protected void SaveConfigButton_OnClick(object sender, EventArgs e)
		{
			var buffer = _encoding.GetBytes(ConfigurationBox.Text);

			if (!string.IsNullOrEmpty(ConfigurationBox.Text))
			{
				_blobStorage.PutBlob(
					AssemblyLoader.ContainerName, AssemblyLoader.ConfigurationBlobName, buffer);
			}
			else
			{
				// don't save empty empty configuration, delete it (so that it will be ignored)
				_blobStorage.DeleteBlob(
					AssemblyLoader.ContainerName, AssemblyLoader.ConfigurationBlobName);
			}

			SavedLabel.Visible = true;
		}
	}
}
