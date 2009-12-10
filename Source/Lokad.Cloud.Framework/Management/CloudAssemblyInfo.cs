#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Cloud Assembly Info
	/// </summary>
	public class CloudAssemblyInfo
	{
		/// <summary>Name of the cloud assembly</summary>
		public string AssemblyName { get; set; }

		/// <summary>Time stamp of the cloud assembly</summary>
		public DateTime DateTime { get; set; }

		/// <summary>Version of the cloud assembly</summary>
		public Version Version { get; set; }

		/// <summary>File size of the cloud assembly</summary>
		public long Size { get; set; }
	}
}
