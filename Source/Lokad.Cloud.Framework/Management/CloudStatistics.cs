#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management.Api10;
using Lokad.Quality;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Management facade for cloud configuration.
	/// </summary>
	[UsedImplicitly]
	public class CloudStatistics : ICloudStatisticsApi 
	{
		readonly ICloudDiagnosticsRepository _repository;

		/// <summary>
		/// Initializes a new instance of the <see cref="CloudStatistics"/> class.
		/// </summary>
		public CloudStatistics(ICloudDiagnosticsRepository diagnosticsRepository)
		{
			_repository = diagnosticsRepository;
		}

		/// <summary>Get the statistics of all cloud partitions on the provided month.</summary>
		public List<PartitionStatistics> GetPartitionsOfMonth(DateTime? monthUtc)
		{
			return _repository.GetAllPartitionStatistics(TimeSegments.For(TimeSegmentPeriod.Month, new DateTimeOffset(monthUtc ?? DateTime.UtcNow, TimeSpan.Zero))).ToList();
		}
		/// <summary>Get the statistics of all cloud partitions on the provided day.</summary>
		public List<PartitionStatistics> GetPartitionsOfDay(DateTime? dayUtc)
		{
			return _repository.GetAllPartitionStatistics(TimeSegments.For(TimeSegmentPeriod.Day, new DateTimeOffset(dayUtc ?? DateTime.UtcNow, TimeSpan.Zero))).ToList();
		}

		/// <summary>Get the statistics of all cloud services on the provided month.</summary>
		public List<ServiceStatistics> GetServicesOfMonth(DateTime? monthUtc)
		{
			return _repository.GetAllServiceStatistics(TimeSegments.For(TimeSegmentPeriod.Month, new DateTimeOffset(monthUtc ?? DateTime.UtcNow, TimeSpan.Zero))).ToList();
		}
		/// <summary>Get the statistics of all cloud services on the provided day.</summary>
		public List<ServiceStatistics> GetServicesOfDay(DateTime? dayUtc)
		{
			return _repository.GetAllServiceStatistics(TimeSegments.For(TimeSegmentPeriod.Day, new DateTimeOffset(dayUtc ?? DateTime.UtcNow, TimeSpan.Zero))).ToList();
		}

		/// <summary>Get the statistics of all execution profiles on the provided month.</summary>
		public List<ExecutionProfilingStatistics> GetProfilesOfMonth(DateTime? monthUtc)
		{
			return _repository.GetExecutionProfilingStatistics(TimeSegments.For(TimeSegmentPeriod.Month, new DateTimeOffset(monthUtc ?? DateTime.UtcNow, TimeSpan.Zero))).ToList();
		}
		/// <summary>Get the statistics of all execution profiles on the provided day.</summary>
		public List<ExecutionProfilingStatistics> GetProfilesOfDay(DateTime? dayUtc)
		{
			return _repository.GetExecutionProfilingStatistics(TimeSegments.For(TimeSegmentPeriod.Day, new DateTimeOffset(dayUtc ?? DateTime.UtcNow, TimeSpan.Zero))).ToList();
		}
	}
}
