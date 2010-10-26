#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Diagnostics Repository
	/// </summary>
	public interface ICloudDiagnosticsRepository
	{
		/// <summary>Get the statistics of all execution profiles.</summary>
		IEnumerable<ExecutionProfilingStatistics> GetExecutionProfilingStatistics(string timeSegment);
		/// <summary>Update the statistics of an execution profile.</summary>
		void UpdateExecutionProfilingStatistics(string timeSegment, string contextName, Func<Maybe<ExecutionProfilingStatistics>, ExecutionProfilingStatistics> updater);
		/// <summary>Remove old statistics of execution profiles.</summary>
		void RemoveExecutionProfilingStatistics(string timeSegmentPrefix, string timeSegmentBefore);

		/// <summary>Get the statistics of all cloud partitions.</summary>
		IEnumerable<PartitionStatistics> GetAllPartitionStatistics(string timeSegment);
		/// <summary>Update the statistics of a cloud partition.</summary>
		void UpdatePartitionStatistics(string timeSegment, string partitionName, Func<Maybe<PartitionStatistics>, PartitionStatistics> updater);
		/// <summary>Remove old statistics of cloud partitions.</summary>
		void RemovePartitionStatistics(string timeSegmentPrefix, string timeSegmentBefore);

		/// <summary>Get the statistics of all cloud services.</summary>
		IEnumerable<ServiceStatistics> GetAllServiceStatistics(string timeSegment);
		/// <summary>Update the statistics of a cloud service.</summary>
		void UpdateServiceStatistics(string timeSegment, string serviceName, Func<Maybe<ServiceStatistics>, ServiceStatistics> updater);
		/// <summary>Remove old statistics of cloud services.</summary>
		void RemoveServiceStatistics(string timeSegmentPrefix, string timeSegmentBefore);
	}
}
