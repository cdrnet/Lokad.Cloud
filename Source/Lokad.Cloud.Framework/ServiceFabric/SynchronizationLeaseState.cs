#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud.ServiceFabric
{
	/// <summary>
	/// Synchronization Lease
	/// </summary>
	[Serializable, DataContract]
	public class SynchronizationLeaseState
	{
		/// <summary>
		/// Point of time when the lease was originally acquired. This value is not
		/// updated when a lease is renewed.
		/// </summary>
		[DataMember]
		public DateTimeOffset Acquired { get; set; }

		/// <summary>
		/// Point of them when the lease will time out and can thus be taken over and
		/// acquired by a new owner.
		/// </summary>
		[DataMember]
		public DateTimeOffset Timeout { get; set; }

		/// <summary>
		/// Reference of the owner of this lease.
		/// </summary>
		[DataMember(IsRequired = false, EmitDefaultValue = false)]
		public string Owner { get; set; }
	}
}
