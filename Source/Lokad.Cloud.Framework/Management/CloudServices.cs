#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Services;
using Lokad.Cloud.Storage;

// TODO: blobs are sequentially enumerated, performance issue
// if there are more than a few dozen services

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// Management facade for cloud services.
	/// </summary>
	public class CloudServices
	{
		readonly IBlobStorageProvider _blobProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="CloudServices"/> class.
		/// </summary>
		public CloudServices(IBlobStorageProvider blobStorageProvider)
		{
			_blobProvider = blobStorageProvider;
		}

		/// <summary>
		/// Enumerate infos of all cloud services.
		/// </summary>
		public IEnumerable<ServiceInfo> GetServices()
		{
			foreach (var blobRef in _blobProvider.List(CloudServiceStateReference.GetPrefix()))
			{
				var blob = _blobProvider.GetBlobOrDelete(blobRef);
				if (!blob.HasValue)
				{
					continue;
				}

				var state = blob.Value;
				yield return new ServiceInfo
					{
						ServiceName = blobRef.ServiceName,
						State = state
					};
			}
		}

		/// <summary>
		/// Enumerate the names of all cloud services.
		/// </summary>
		public IEnumerable<string> GetServiceNames()
		{
			return _blobProvider.List(CloudServiceStateReference.GetPrefix())
				.Select(reference => reference.ServiceName);
		}

		/// <summary>
		/// Enumerate the names of all user cloud services (system services are skipped).
		/// </summary>
		public IEnumerable<string> GetUserServiceNames()
		{
			var systemServices =
				new[]
					{
						typeof(GarbageCollectorService),
						typeof(DelayedQueueService),
						typeof(MonitoringService),
						typeof(MonitoringDataRetentionService)
					}
					.Select(type => type.FullName)
					.ToList();

			return GetServiceNames()
				.Where(service => !systemServices.Contains(service));
		}

		/// <summary>
		/// Enable a cloud service
		/// </summary>
		public void EnableService(string serviceName)
		{
			var blobRef = new CloudServiceStateReference(serviceName);
			_blobProvider.PutBlob(blobRef, CloudServiceState.Started);
		}

		/// <summary>
		/// Disable a cloud service
		/// </summary>
		public void DisableService(string serviceName)
		{
			var blobRef = new CloudServiceStateReference(serviceName);
			_blobProvider.PutBlob(blobRef, CloudServiceState.Stopped);
		}

		/// <summary>
		/// Toggle the state of a cloud service
		/// </summary>
		public void ToggleServiceState(string serviceName)
		{
			var blobRef = new CloudServiceStateReference(serviceName);
			_blobProvider.UpdateIfNotModified(
				blobRef,
				s => s.HasValue
					? (s.Value == CloudServiceState.Started ? CloudServiceState.Stopped : CloudServiceState.Started)
					: CloudServiceState.Started);
		}

		/// <summary>
		/// Remove the state information of a cloud service
		/// </summary>
		public void RemoveServiceState(string serviceName)
		{
			var blobRef = new CloudServiceStateReference(serviceName);
			_blobProvider.DeleteBlob(blobRef);
		}
	}
}
