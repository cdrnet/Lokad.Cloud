#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

namespace Lokad.Cloud
{
	/// <summary>Schedule settings for the execution of a <see cref="ScheduledService"/>.</summary>
	/// <remarks>The implementation is kept very simple for now. Complete scheduling,
	/// specifing specific hours or days will be added later on.</remarks>
	public sealed class ScheduledServiceSettingsAttribute : CloudServiceSettingsAttribute
	{
		/// <summary>Indicates the interval between the scheduled executions
		/// (expressed in seconds).</summary>
		/// <remarks><c>TimeSpan</c> cannot be used here, because it's not compatible
		/// with the attribute usage.</remarks>
		public double TriggerInterval { get; set; }
	}
}
