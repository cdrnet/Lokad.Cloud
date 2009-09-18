#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
	/// <summary>Shared settings for all <see cref="CloudService"/>s.</summary>
	[AttributeUsageAttribute(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class CloudServiceSettingsAttribute : Attribute
	{
		/// <summary>Indicates whether the service is be started by default
		/// when the cloud app is deployed.</summary>
		public bool AutoStart { get; set; }

		/// <summary>Define the relative priority of this service compared to the
		/// other services.</summary>
		public double Priority { get; set; }

		/// <summary>Gets a description of the service (for administration purposes).</summary>
		public string Description { get; set; }

		/// <summary>Execution time-out for the <c>StartImpl</c> methods of 
		/// <see cref="CloudService"/> inheritors.</summary>
		public TimeSpan ProcessingTimeout { get; set; }
	}
}
