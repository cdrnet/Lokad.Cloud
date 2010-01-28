#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using Lokad.Cloud.Services;

// TODO: blobs are sequentially enumerated, performance issue
// if there are more than a few dozen services

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Management facade for scheduled cloud services.
	/// </summary>
	public class CloudServiceScheduling
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
		public IEnumerable<ServiceSchedulingInfo> GetSchedules()
		{
			foreach (var blobRef in _blobProvider.List(ScheduledServiceStateReference.GetPrefix()))
			{
				var blob = _blobProvider.GetBlobOrDelete(blobRef);
				if (!blob.HasValue)
				{
					continue;
				}

				var state = blob.Value;
				var info =  new ServiceSchedulingInfo
					{
						ServiceName = blobRef.ServiceName,
						TriggerInterval = state.TriggerInterval,
						LastExecuted = state.LastExecuted,
						WorkerScoped = state.SchedulePerWorker,
						LeasedBy = Maybe.String,
						LeasedSince = Maybe<DateTimeOffset>.Empty,
						LeasedUntil = Maybe<DateTimeOffset>.Empty
					};

				if(state.Lease != null)
				{
					info.LeasedBy = state.Lease.Owner;
					info.LeasedSince = state.Lease.Acquired;
					info.LeasedUntil = state.Lease.Timeout;
				}

				yield return info;
			}
		}

		/// <summary>
		/// Enumerate the names of all scheduled cloud service.
		/// </summary>
		public IEnumerable<string> GetScheduledServiceNames()
		{
			return _blobProvider.List(ScheduledServiceStateReference.GetPrefix())
				.Select(reference => reference.ServiceName);
		}

		/// <summary>
		/// Enumerate the names of all scheduled user cloud service (system services are skipped).
		/// </summary>
		public IEnumerable<string> GetScheduledUserServiceNames()
		{
			var systemServices =
				new[] { typeof(GarbageCollectorService), typeof(DelayedQueueService), typeof(MonitoringService) }
					.Select(type => type.FullName)
					.ToList();

			return GetScheduledServiceNames()
				.Where(service => !systemServices.Contains(service));
		}

		/// <summary>
		/// Set the trigger interval of a cloud service.
		/// </summary>
		public void SetTriggerInterval(string serviceName, TimeSpan triggerInterval)
		{
			var blobRef = new ScheduledServiceStateReference(serviceName);
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
		public void RemoveSchedule(string serviceName)
		{
			var blobRef = new ScheduledServiceStateReference(serviceName);
			_blobProvider.DeleteBlob(blobRef);
		}


		/// <summary>
		/// Forcibly remove the synchronization lease of a periodic cloud service
		/// </summary>
		public void ReleaseLease(string serviceName)
		{
			var blobRef = new ScheduledServiceStateReference(serviceName);
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
