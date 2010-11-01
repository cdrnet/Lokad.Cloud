﻿#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using Lokad.Diagnostics.Persist;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Extension interface for custom or external diagnostics providers.
	/// </summary>
	/// <remarks>
	/// If a diagnostics source is registered in IoC as member of
	/// ICloudDiagnosticsSource, it will be queried by the diagnostics
	/// infrastructure in regular intervals.
	/// </remarks>
	public interface ICloudDiagnosticsSource
	{
		void GetIncrementalStatistics(Action<string, IEnumerable<ExecutionData>> pushExecutionProfiles);
	}
	
}
