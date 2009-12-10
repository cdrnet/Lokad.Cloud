#region Copyright (c) Lokad 2009
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
			foreach (var blobName in _blobProvider.List(ScheduledServiceStateName.GetPrefix()))
			{
				var blob = _blobProvider.GetBlobOrDelete(blobName);
				if (!blob.HasValue)
				{
					continue;
				}

				var state = blob.Value;
				yield return new ServiceSchedulingInfo
					{
						ServiceName = blobName.ServiceName,
						TriggerInterval = state.TriggerInterval,
						LastExecuted = state.LastExecuted
					};
			}
		}

		/// <summary>
		/// Enumerate the names of all scheduled cloud service.
		/// </summary>
		public IEnumerable<string> GetScheduledServiceNames()
		{
			return _blobProvider.List(ScheduledServiceStateName.GetPrefix())
				.Select(name => name.ServiceName);
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
			var blobName = new ScheduledServiceStateName(serviceName);

			_blobProvider.UpdateIfNotModified(blobName,
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
			var blobName = new ScheduledServiceStateName(serviceName);

			_blobProvider.DeleteBlob(blobName);
		}
	}
}
