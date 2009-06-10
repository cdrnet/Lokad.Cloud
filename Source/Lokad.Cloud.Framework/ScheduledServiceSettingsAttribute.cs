#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

namespace Lokad.Cloud.Framework
{
	/// <summary>Schedule settings for the execution of a <see cref="ScheduledService"/>.</summary>
	/// <remarks>The implementation is kept very simple for now. Complete scheduling,
	/// specifing specific hours or days will be added later on.</remarks>
	public sealed class ScheduledServiceSettingsAttribute : CloudServiceSettingsAttribute
	{
		/// <summary>Indicates the interval between the scheduled executions.</summary>
		public TimeSpan TriggerInterval { get; set; }
	}
}
