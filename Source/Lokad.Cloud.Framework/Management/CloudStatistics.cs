#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
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
		public IEnumerable<PartitionStatistics> GetPartitionsInPeriod(TimeSegmentPeriod period, DateTimeOffset date)
		{
			return _repository.GetAllPartitionStatistics(TimeSegments.For(period, date));
		}
		/// <summary>Get the statistics of all cloud partitions.</summary>
		public IEnumerable<PartitionStatistics> GetAllPartitions()
		{
			return _repository.GetAllPartitionStatistics(TimeSegments.MonthPrefix);
		}

		/// <summary>Get the statistics of monthly cloud services.</summary>
		public IEnumerable<ServiceStatistics> GetServicesInPeriod(TimeSegmentPeriod period, DateTimeOffset date)
		{
			return _repository.GetAllServiceStatistics(TimeSegments.For(period, date));
		}
		/// <summary>Get the statistics of all cloud services.</summary>
		public IEnumerable<ServiceStatistics> GetAllServices()
		{
			return _repository.GetAllServiceStatistics(TimeSegments.MonthPrefix);
		}

		/// <summary>Get the statistics of monthly tracked exceptions.</summary>
		public IEnumerable<ExceptionTrackingStatistics> GetTrackedExceptionsInPeriod(TimeSegmentPeriod period, DateTimeOffset date)
		{
			return _repository.GetExceptionTrackingStatistics(TimeSegments.For(period, date));
		}
		/// <summary>Get the statistics of all tracked exceptions.</summary>
		public IEnumerable<ExceptionTrackingStatistics> GetAllTrackedExceptions()
		{
			return _repository.GetExceptionTrackingStatistics(TimeSegments.MonthPrefix);
		}
		
		/// <summary>Get the statistics of monthly execution profiles.</summary>
		public IEnumerable<ExecutionProfilingStatistics> GetExecutionProfilesInPeriod(TimeSegmentPeriod period, DateTimeOffset date)
		{
			return _repository.GetExecutionProfilingStatistics(TimeSegments.For(period, date));
		}
		/// <summary>Get the statistics of all execution profiles.</summary>
		public IEnumerable<ExecutionProfilingStatistics> GetAllExecutionProfiles()
		{
			return _repository.GetExecutionProfilingStatistics(TimeSegments.MonthPrefix);
		}
	}
}
