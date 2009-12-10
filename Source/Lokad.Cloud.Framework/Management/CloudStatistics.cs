#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Management facade for cloud configuration.
	/// </summary>
	public class CloudStatistics
	{
		readonly ICloudDiagnosticsRepository _repository;

		/// <summary>
		/// Initializes a new instance of the <see cref="CloudStatistics"/> class.
		/// </summary>
		public CloudStatistics(ICloudDiagnosticsRepository diagnosticsRepository)
		{
			_repository = diagnosticsRepository;
		}

		/// <summary>Get the statistics of all cloud partitions.</summary>
		public IEnumerable<PartitionStatistics> GetPartitions()
		{
			return _repository.GetAllPartitionStatistics();
		}

		/// <summary>Get the statistics of all cloud services.</summary>
		public IEnumerable<ServiceStatistics> GetServices()
		{
			return _repository.GetAllServiceStatistics();
		}

		/// <summary>Get the statistics of all tracked exceptions.</summary>
		public IEnumerable<ExceptionTrackingStatistics> GetTrackedExceptions()
		{
			return _repository.GetAllExceptionTrackingStatistics();
		}

		/// <summary>Get the statistics of all execution profiles.</summary>
		public IEnumerable<ExecutionProfilingStatistics> GetExecutionProfiles()
		{
			return _repository.GetAllExecutionProfilingStatistics();
		}
	}
}
