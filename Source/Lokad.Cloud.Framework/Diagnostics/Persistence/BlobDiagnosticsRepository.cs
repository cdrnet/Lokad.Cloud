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
				.Where(x => null != x);
		}

		void Update<T>(BaseBlobName name, Func<T,T> updater)
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
		public IEnumerable<ExceptionTrackingStatistics> GetAllExceptionTrackingStatistics()
		{
			return GetAll<ExceptionTrackingStatistics,ExceptionTrackingStatisticsName>(
				ExceptionTrackingStatisticsName.GetPrefix());
		}

		/// <summary>
		/// Get the statistics of all execution profiles.
		/// </summary>
		public IEnumerable<ExecutionProfilingStatistics> GetAllExecutionProfilingStatistics()
		{
			return GetAll<ExecutionProfilingStatistics, ExecutionProfilingStatisticsName>(
				ExecutionProfilingStatisticsName.GetPrefix());
		}

		/// <summary>
		/// Get the statistics of all cloud partitions.
		/// </summary>
		public IEnumerable<PartitionStatistics> GetAllPartitionStatistics()
		{
			return GetAll<PartitionStatistics, PartitionStatisticsName>(
				PartitionStatisticsName.GetPrefix());
		}

		/// <summary>
		/// Get the statistics of all cloud services.
		/// </summary>
		public IEnumerable<ServiceStatistics> GetAllServiceStatistics()
		{
			return GetAll<ServiceStatistics, ServiceStatisticsName>(
				ServiceStatisticsName.GetPrefix());
		}

		/// <summary>
		/// Update the statistics of a tracked exception.
		/// </summary>
		public void UpdateExceptionTrackingStatistics(string contextName, Func<ExceptionTrackingStatistics, ExceptionTrackingStatistics> updater)
		{
			Update(ExceptionTrackingStatisticsName.New(contextName), updater);
		}

		/// <summary>
		/// Update the statistics of an execution profile.
		/// </summary>
		public void UpdateExecutionProfilingStatistics(string contextName, Func<ExecutionProfilingStatistics, ExecutionProfilingStatistics> updater)
		{
			Update(ExecutionProfilingStatisticsName.New(contextName), updater);
		}

		/// <summary>
		/// Update the statistics of a cloud partition.
		/// </summary>
		public void UpdatePartitionStatistics(string partitionName, Func<PartitionStatistics, PartitionStatistics> updater)
		{
			Update(PartitionStatisticsName.New(partitionName), updater);
		}

		/// <summary>
		/// Set the statistics of a cloud partition.
		/// </summary>
		public void SetPartitionStatistics(string partitionName, PartitionStatistics statistics)
		{
			Set(PartitionStatisticsName.New(partitionName), statistics);
		}

		/// <summary>
		/// Update the statistics of a cloud service.
		/// </summary>
		public void UpdateServiceStatistics(string serviceName, Func<ServiceStatistics, ServiceStatistics> updater)
		{
			Update(ServiceStatisticsName.New(serviceName), updater);
		}
	}
}
