#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Cloud Service Info
	/// </summary>
	public class ServiceInfo
	{
		/// <summary>Name of the service</summary>
		public string ServiceName { get; set; }

		/// <summary>Current state of the service</summary>
		public CloudServiceState State { get; set; }
	}
}
