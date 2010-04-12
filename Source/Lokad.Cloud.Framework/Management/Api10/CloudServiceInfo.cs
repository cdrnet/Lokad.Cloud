#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Runtime.Serialization;

namespace Lokad.Cloud.Management.Api10
{
	/// <summary>
	/// Cloud Service Info
	/// </summary>
	[DataContract(Name = "CloudServiceInfo", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public class CloudServiceInfo
	{
		/// <summary>Name of the service</summary>
		[DataMember(Order = 0, IsRequired = true)]
		public string ServiceName { get; set; }

		/// <summary>Current state of the service</summary>
		[DataMember(Order = 1)]
		public bool IsStarted { get; set; }
	}
}