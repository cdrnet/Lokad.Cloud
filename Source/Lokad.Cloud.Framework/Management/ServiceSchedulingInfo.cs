#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Cloud Service Scheduling Info
	/// </summary>
	public class ServiceSchedulingInfo
	{
		/// <summary>Name of the service</summary>
		public string ServiceName { get; set; }

		/// <summary>Scheduled trigger interval</summary>
		public TimeSpan TriggerInterval { get; set; }

		/// <summary>Last execution time stamp</summary>
		public DateTimeOffset LastExecuted { get; set; }

		/// <summary>True if the services is worker scoped instead of cloud scoped.</summary>
		public bool WorkerScoped { get; set; }
	}
}
