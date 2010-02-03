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
	/// Facade to collect internal and external diagnostics statistics (pull or push)
	/// </summary>
	public class DiagnosticsAcquisition
	{
		readonly ExecutionProfilingMonitor _executionProfiling;
		readonly ExceptionTrackingMonitor _exceptionTracking;
		readonly PartitionMonitor _partitionMonitor;
		readonly ServiceMonitor _serviceMonitor;

		/// <remarks>IoC Injected, but optional</remarks>
		public ICloudDiagnosticsSource DiagnosticsSource { get; set; }

		public DiagnosticsAcquisition(ICloudDiagnosticsRepository repository)
		{
			_executionProfiling = new ExecutionProfilingMonitor(repository);
			_exceptionTracking = new ExceptionTrackingMonitor(repository);
			_partitionMonitor = new PartitionMonitor(repository);
			_serviceMonitor = new ServiceMonitor(repository);
		}
		
		/// <summary>
		/// Collect (pull) internal and external diagnostics statistics and persists
		/// them in the diagnostics repository.
		/// </summary>
		public void CollectStatistics()
		{
			_executionProfiling.UpdateDefaultStatistics();
			_exceptionTracking.UpdateDefaultStatistics();

			_partitionMonitor.UpdateStatistics();
			_serviceMonitor.UpdateStatistics();

			if (DiagnosticsSource != null)
			{
				DiagnosticsSource.GetIncrementalStatistics(
					_executionProfiling.Update,
					_exceptionTracking.Update);
			}
		}

		/// <summary>
		/// Remove all statistics older than the provided time stamp from the
		/// persistent diagnostics repository.
		/// </summary>
		public void RemoveStatisticsBefore(DateTimeOffset before)
		{
			_executionProfiling.RemoveStatisticsBefore(before);
			_exceptionTracking.RemoveStatisticsBefore(before);
			_partitionMonitor.RemoveStatisticsBefore(before);
			_serviceMonitor.RemoveStatisticsBefore(before);
		}

		/// <summary>
		/// Remove all statistics older than the provided number of periods from the
		/// persistent diagnostics repository (0 removes all but the current period).
		/// </summary>
		public void RemoveStatisticsBefore(int numberOfPeriods)
		{
			_executionProfiling.RemoveStatisticsBefore(numberOfPeriods);
			_exceptionTracking.RemoveStatisticsBefore(numberOfPeriods);
			_partitionMonitor.RemoveStatisticsBefore(numberOfPeriods);
			_serviceMonitor.RemoveStatisticsBefore(numberOfPeriods);
		}

		/// <summary>
		/// Push incremental statistics for external execution profiles.
		/// </summary>
		public void PushExecutionProfilingStatistics(string context, IEnumerable<ExecutionData> additionalData)
		{
			_executionProfiling.Update(context, additionalData);
		}

		/// <summary>
		/// Push incremental statistics for externally tracked exceptions.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="additionalData"></param>
		public void PushTrackedExceptionStatistics(string context, IEnumerable<ExceptionData> additionalData)
		{
			_exceptionTracking.Update(context, additionalData);
		}
	}
}
