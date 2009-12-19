#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	/// <summary>
	/// Diagnostics Cloud Data Repository to Blob Storage
	/// </summary>
	public class BlobDiagnosticsRepository : ICloudDiagnosticsRepository
	{
		readonly IBlobStorageProvider _provider;

		/// <summary>
		/// Creates an Instance of the <see cref="BlobDiagnosticsRepository"/> class.
		/// </summary>
		public BlobDiagnosticsRepository(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		IEnumerable<T> GetAll<T, TName>(BlobNamePrefix<TName> prefix)
			where TName : BaseTypedBlobName<T>
			where T : class
		{
			return _provider
				.List(prefix)
				.Select(name => _provider.GetBlobOrDelete(name))
				.Where(x => x.HasValue)
				.Select(x => x.Value);
		}

		void Update<T>(BaseBlobName name, Func<Maybe<T>,T> updater)
		{
			T result;
			_provider.AtomicUpdate(
				name,
				updater,
				out result);
		}

		void Set<T>(BaseBlobName name, T value)
		{
			_provider.PutBlob(
				name,
				value,
				true);
		}

		/// <summary>
		/// Get the statistics of all tracked exceptions.
		/// </summary>
		public IEnumerable<ExceptionTrackingStatistics> GetExceptionTrackingStatistics(string timeSegment)
		{
			return GetAll<ExceptionTrackingStatistics,ExceptionTrackingStatisticsName>(
				ExceptionTrackingStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all execution profiles.
		/// </summary>
		public IEnumerable<ExecutionProfilingStatistics> GetExecutionProfilingStatistics(string timeSegment)
		{
			return GetAll<ExecutionProfilingStatistics, ExecutionProfilingStatisticsName>(
				ExecutionProfilingStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all cloud partitions.
		/// </summary>
		public IEnumerable<PartitionStatistics> GetAllPartitionStatistics(string timeSegment)
		{
			return GetAll<PartitionStatistics, PartitionStatisticsName>(
				PartitionStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all cloud services.
		/// </summary>
		public IEnumerable<ServiceStatistics> GetAllServiceStatistics(string timeSegment)
		{
			return GetAll<ServiceStatistics, ServiceStatisticsName>(
				ServiceStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Update the statistics of a tracked exception.
		/// </summary>
		public void UpdateExceptionTrackingStatistics(string timerSegment, string contextName, Func<Maybe<ExceptionTrackingStatistics>, ExceptionTrackingStatistics> updater)
		{
			Update(ExceptionTrackingStatisticsName.New(timerSegment, contextName), updater);
		}

		/// <summary>
		/// Update the statistics of an execution profile.
		/// </summary>
		public void UpdateExecutionProfilingStatistics(string timerSegment, string contextName, Func<Maybe<ExecutionProfilingStatistics>, ExecutionProfilingStatistics> updater)
		{
			Update(ExecutionProfilingStatisticsName.New(timerSegment, contextName), updater);
		}

		/// <summary>
		/// Update the statistics of a cloud partition.
		/// </summary>
		public void UpdatePartitionStatistics(string timeSegment, string partitionName, Func<Maybe<PartitionStatistics>, PartitionStatistics> updater)
		{
			Update(PartitionStatisticsName.New(timeSegment, partitionName), updater);
		}

		/// <summary>
		/// Set the statistics of a cloud partition.
		/// </summary>
		public void SetPartitionStatistics(string timeSegment, string partitionName, PartitionStatistics statistics)
		{
			Set(PartitionStatisticsName.New(timeSegment, partitionName), statistics);
		}

		/// <summary>
		/// Update the statistics of a cloud service.
		/// </summary>
		public void UpdateServiceStatistics(string timeSegment, string serviceName, Func<Maybe<ServiceStatistics>, ServiceStatistics> updater)
		{
			Update(ServiceStatisticsName.New(timeSegment, serviceName), updater);
		}
	}
}
