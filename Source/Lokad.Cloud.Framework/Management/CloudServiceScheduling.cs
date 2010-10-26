#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using Lokad.Cloud.Management.Api10;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Services;
using Lokad.Cloud.Storage;
using Lokad.Quality;

// TODO: blobs are sequentially enumerated, performance issue
// if there are more than a few dozen services

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Management facade for scheduled cloud services.
	/// </summary>
	[UsedImplicitly]
	public class CloudServiceScheduling : ICloudServiceSchedulingApi
	{
		readonly IBlobStorageProvider _blobProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="CloudServiceScheduling"/> class.
		/// </summary>
		public CloudServiceScheduling(IBlobStorageProvider blobStorageProvider)
		{
			_blobProvider = blobStorageProvider;
		}

		/// <summary>
		/// Enumerate infos of all cloud service schedules.
		/// </summary>
		public List<CloudServiceSchedulingInfo> GetSchedules()
		{
			return _blobProvider.List(ScheduledServiceStateName.GetPrefix())
				.Select(blobRef => Tuple.From(blobRef, _blobProvider.GetBlobOrDelete(blobRef)))
				.Where(pair => pair.Value.HasValue)
				.Select(pair =>
					{
						var state = pair.Value.Value;
						var info = new CloudServiceSchedulingInfo
							{
								ServiceName = pair.Key.ServiceName,
								TriggerInterval = state.TriggerInterval,
								LastExecuted = state.LastExecuted,
								WorkerScoped = state.SchedulePerWorker,
								LeasedBy = Maybe.String,
								LeasedSince = Maybe<DateTimeOffset>.Empty,
								LeasedUntil = Maybe<DateTimeOffset>.Empty
							};

						if (state.Lease != null)
						{
							info.LeasedBy = state.Lease.Owner;
							info.LeasedSince = state.Lease.Acquired;
							info.LeasedUntil = state.Lease.Timeout;
						}

						return info;
					})
				.ToList();
		}

		/// <summary>
		/// Gets infos of one cloud service schedule.
		/// </summary>
		public CloudServiceSchedulingInfo GetSchedule(string serviceName)
		{
			var blob = _blobProvider.GetBlob(new ScheduledServiceStateName(serviceName));

			var state = blob.Value;
			var info = new CloudServiceSchedulingInfo
			{
				ServiceName = serviceName,
				TriggerInterval = state.TriggerInterval,
				LastExecuted = state.LastExecuted,
				WorkerScoped = state.SchedulePerWorker,
				LeasedBy = Maybe.String,
				LeasedSince = Maybe<DateTimeOffset>.Empty,
				LeasedUntil = Maybe<DateTimeOffset>.Empty
			};

			if (state.Lease != null)
			{
				info.LeasedBy = state.Lease.Owner;
				info.LeasedSince = state.Lease.Acquired;
				info.LeasedUntil = state.Lease.Timeout;
			}

			return info;
		}

		/// <summary>
		/// Enumerate the names of all scheduled cloud service.
		/// </summary>
		public List<string> GetScheduledServiceNames()
		{
			return _blobProvider.List(ScheduledServiceStateName.GetPrefix())
				.Select(reference => reference.ServiceName).ToList();
		}

		/// <summary>
		/// Enumerate the names of all scheduled user cloud service (system services are skipped).
		/// </summary>
		public List<string> GetScheduledUserServiceNames()
		{
			var systemServices =
				new[]
					{
						typeof(GarbageCollectorService),
						typeof(DelayedQueueService),
						typeof(MonitoringService),
						typeof(MonitoringDataRetentionService),
						typeof(AssemblyConfigurationUpdateService)
					}
					.Select(type => type.FullName)
					.ToList();

			return GetScheduledServiceNames()
				.Where(service => !systemServices.Contains(service)).ToList();
		}

		/// <summary>
		/// Set the trigger interval of a cloud service.
		/// </summary>
		public void SetTriggerInterval(string serviceName, TimeSpan triggerInterval)
		{
			var blobRef = new ScheduledServiceStateName(serviceName);
			_blobProvider.UpdateIfNotModified(blobRef,
				blob =>
				{
					var state = blob.Value;
					state.TriggerInterval = triggerInterval;
					return state;
				});
		}

		/// <summary>
		/// Remove the scheduling information of a cloud service
		/// </summary>
		public void ResetSchedule(string serviceName)
		{
			var blobRef = new ScheduledServiceStateName(serviceName);
			_blobProvider.DeleteBlob(blobRef);
		}

		/// <summary>
		/// Forcibly remove the synchronization lease of a periodic cloud service
		/// </summary>
		public void ReleaseLease(string serviceName)
		{
			var blobRef = new ScheduledServiceStateName(serviceName);
			_blobProvider.UpdateIfNotModified(
				blobRef,
				currentState =>
				{
					if (!currentState.HasValue)
					{
						return Result<ScheduledServiceState>.CreateError("No service state available.");
					}

					var state = currentState.Value;

					// remove lease
					state.Lease = null;
					return Result.CreateSuccess(state);
				});
		}
	}
}
