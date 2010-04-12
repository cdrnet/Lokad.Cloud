#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Management.Api10
{
	/// <summary>
	/// Cloud Service Scheduling Info
	/// </summary>
	[DataContract(Name = "CloudServiceSchedulingInfo", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public class CloudServiceSchedulingInfo
	{
		/// <summary>Name of the service.</summary>
		[DataMember(Order = 0, IsRequired = true)]
		public string ServiceName { get; set; }

		/// <summary>Scheduled trigger interval.</summary>
		[DataMember(Order = 1, IsRequired = true)]
		public TimeSpan TriggerInterval { get; set; }

		/// <summary>Last execution time stamp.</summary>
		[DataMember(Order = 3, IsRequired = false)]
		public DateTimeOffset LastExecuted { get; set; }

		/// <summary>True if the services is worker scoped instead of cloud scoped.</summary>
		[DataMember(Order = 2, IsRequired = false, EmitDefaultValue = false)]
		public bool WorkerScoped { get; set; }

		/// <summary>Owner of the lease.</summary>
		[DataMember(Order = 4, IsRequired = false)]
		public Maybe<string> LeasedBy { get; set; }

		/// <summary>Point of time when the lease was acquired.</summary>
		[DataMember(Order = 5, IsRequired = false)]
		public Maybe<DateTimeOffset> LeasedSince { get; set; }

		/// <summary>Point of time when the lease will timeout.</summary>
		[DataMember(Order = 6, IsRequired = false)]
		public Maybe<DateTimeOffset> LeasedUntil { get; set; }
	}
}