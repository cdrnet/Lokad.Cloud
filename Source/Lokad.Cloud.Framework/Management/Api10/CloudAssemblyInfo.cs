#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Management.Api10
{
	/// <summary>
	/// Cloud Assembly Info
	/// </summary>
	[DataContract(Name = "CloudAssemblyInfo", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public class CloudAssemblyInfo
	{
		/// <summary>Name of the cloud assembly.</summary>
		[DataMember(Order = 0, IsRequired = true)]
		public string AssemblyName { get; set; }

		/// <summary>Time stamp of the cloud assembly.</summary>
		[DataMember(Order = 1)]
		public DateTime DateTime { get; set; }

		/// <summary>Version of the cloud assembly.</summary>
		[DataMember(Order = 2)]
		public Version Version { get; set; }

		/// <summary>File size of the cloud assembly, in bytes.</summary>
		[DataMember(Order = 3)]
		public long SizeBytes { get; set; }

		/// <summary>Assembly can be loaded successfully.</summary>
		[DataMember(Order = 4)]
		public bool IsValid { get; set; }

		/// <summary>Assembly symbol store (PDB file) is available.</summary>
		[DataMember(Order = 5)]
		public bool HasSymbols { get; set; }
	}
}