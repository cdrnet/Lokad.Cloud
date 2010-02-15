#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Web
{
	public partial class Assemblies : System.Web.UI.Page
	{
		readonly CloudAssemblies _cloudAssemblies = GlobalSetup.Container.Resolve<CloudAssemblies>();
		readonly ILog _log = GlobalSetup.Container.Resolve<ILog>();

		protected void Page_Load(object sender, EventArgs e)
		{
			AssembliesView.DataBind();
		}

		protected void AssembliesView_DataBinding(object sender, EventArgs e)
		{
			List<CloudAssemblyInfo> assemblyInfos;
			try
			{
				assemblyInfos = _cloudAssemblies.GetAssemblies().ToList();
			}
			catch (Exception ex)
			{
				_log.Error(ex, "Assembly package failed to load in the management UI.");
				_assemblyWarningPanel.Visible = true;
				AssembliesView.DataSource = null;
				return;
			}

			_assemblyWarningPanel.Visible = assemblyInfos.Exists(a => !a.IsValid);
			AssembliesView.DataSource = assemblyInfos
				.Select(info => new
					{
						Name = info.AssemblyName,
						info.DateTime,
						Version = info.Version.ToString(),
						Size = PrettyFormatMemory(info.SizeBytes),
						Symbols = info.HasSymbols ? "Available" : "None",
						Status = info.IsValid ? "OK" : "Corrupt",
					});
		}

		protected void UploadButton_Click(object sender, EventArgs e)
		{
			Page.Validate("Upload");
			if(!Page.IsValid)
			{
				return;
			}

			// defensive design, the validator should prevent this
			if(!AssemblyFileUpload.HasFile)
			{
				return;
			}

			string extension = GetLowercaseExtension(AssemblyFileUpload.FileName);

			// If the file is a DLL, it must be compressed as ZIP
			if(extension == ".dll")
			{
				_cloudAssemblies.SetAssemblyDll(
					AssemblyFileUpload.FileBytes,
					AssemblyFileUpload.FileName);
			}
			else
			{
				_cloudAssemblies.SetAssemblyZipContainer(
					AssemblyFileUpload.FileBytes);
			}

			AssembliesView.DataBind();

			UploadSucceededLabel.Visible = true;
		}

		private static string GetLowercaseExtension(string fileName)
		{
			string extension = Path.GetExtension(fileName);
			if (extension == null)
			{
				return "";
			}

			return extension.ToLowerInvariant();
		}

		protected void UploadValidator_Validate(object source, ServerValidateEventArgs args)
		{
			// file must exists
			if (!AssemblyFileUpload.HasFile)
			{
				args.IsValid = false;
				return;
			}

			// Extension must be ".zip" or ".dll"
			var extension = GetLowercaseExtension(AssemblyFileUpload.FileName);
			if (extension != ".zip" && extension != ".dll")
			{
				args.IsValid = false;
				return;
			}

			// In case of zip, checking that the archive can be decompressed correctly.
			if (extension == ".zip" && !_cloudAssemblies.IsValidZipContainer(AssemblyFileUpload.FileBytes))
			{
				args.IsValid = false;
				return;
			}

			args.IsValid = true;
		}

		static string PrettyFormatMemory(long byteCount)
		{
			return String.Format("{0} KB", byteCount / 1024);
		}
	}
}
