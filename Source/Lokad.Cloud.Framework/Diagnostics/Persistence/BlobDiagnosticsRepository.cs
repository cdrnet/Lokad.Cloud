﻿#region Copyright (c) Lokad 2009
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

		IEnumerable<T> GetAll<T, TReference>(BlobNamePrefix<TReference> prefix)
			where TReference : BlobReference<T>
			where T : class
		{
			return _provider
				.List(prefix)
				.Select(reference => _provider.GetBlobOrDelete(reference))
				.Where(x => x.HasValue)
				.Select(x => x.Value);
		}

		void Update<T>(BlobReference<T> reference, Func<Maybe<T>, T> updater)
		{
			T result;
			_provider.AtomicUpdate(
				reference,
				updater,
				out result);
		}

		void Set<T>(BlobReference<T> reference, T value)
		{
			_provider.PutBlob(
				reference,
				value,
				true);
		}

		/// <summary>
		/// Get the statistics of all tracked exceptions.
		/// </summary>
		public IEnumerable<ExceptionTrackingStatistics> GetExceptionTrackingStatistics(string timeSegment)
		{
			return GetAll<ExceptionTrackingStatistics,ExceptionTrackingStatisticsReference>(
				ExceptionTrackingStatisticsReference.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all execution profiles.
		/// </summary>
		public IEnumerable<ExecutionProfilingStatistics> GetExecutionProfilingStatistics(string timeSegment)
		{
			return GetAll<ExecutionProfilingStatistics, ExecutionProfilingStatisticsReference>(
				ExecutionProfilingStatisticsReference.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all cloud partitions.
		/// </summary>
		public IEnumerable<PartitionStatistics> GetAllPartitionStatistics(string timeSegment)
		{
			return GetAll<PartitionStatistics, PartitionStatisticsReference>(
				PartitionStatisticsReference.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all cloud services.
		/// </summary>
		public IEnumerable<ServiceStatistics> GetAllServiceStatistics(string timeSegment)
		{
			return GetAll<ServiceStatistics, ServiceStatisticsReference>(
				ServiceStatisticsReference.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Update the statistics of a tracked exception.
		/// </summary>
		public void UpdateExceptionTrackingStatistics(string timerSegment, string contextName, Func<Maybe<ExceptionTrackingStatistics>, ExceptionTrackingStatistics> updater)
		{
			Update(ExceptionTrackingStatisticsReference.New(timerSegment, contextName), updater);
		}

		/// <summary>
		/// Update the statistics of an execution profile.
		/// </summary>
		public void UpdateExecutionProfilingStatistics(string timerSegment, string contextName, Func<Maybe<ExecutionProfilingStatistics>, ExecutionProfilingStatistics> updater)
		{
			Update(ExecutionProfilingStatisticsReference.New(timerSegment, contextName), updater);
		}

		/// <summary>
		/// Update the statistics of a cloud partition.
		/// </summary>
		public void UpdatePartitionStatistics(string timeSegment, string partitionName, Func<Maybe<PartitionStatistics>, PartitionStatistics> updater)
		{
			Update(PartitionStatisticsReference.New(timeSegment, partitionName), updater);
		}

		/// <summary>
		/// Update the statistics of a cloud service.
		/// </summary>
		public void UpdateServiceStatistics(string timeSegment, string serviceName, Func<Maybe<ServiceStatistics>, ServiceStatistics> updater)
		{
			Update(ServiceStatisticsReference.New(timeSegment, serviceName), updater);
		}
	}
}
