#region Copyright (c) Lokad 2009-2010
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
		/// <summary>Name of the cloud assembly.</summary>
		public string AssemblyName { get; set; }

		/// <summary>Time stamp of the cloud assembly.</summary>
		public DateTime DateTime { get; set; }

		/// <summary>Version of the cloud assembly.</summary>
		public Version Version { get; set; }

		/// <summary>File size of the cloud assembly, in bytes.</summary>
		public long SizeBytes { get; set; }

		/// <summary>Assembly can be loaded successfully.</summary>
		public bool IsValid { get; set; }

		/// <summary>Assembly symbol store (PDB file) is available.</summary>
		public bool HasSymbols { get; set; }
	}
}
