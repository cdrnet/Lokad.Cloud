#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.Linq;
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
	/// Management facade for cloud services.
	/// </summary>
	[UsedImplicitly]
	public class CloudServices : ICloudServicesApi
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
		public List<CloudServiceInfo> GetServices()
		{
			return _blobProvider.List(CloudServiceStateReference.GetPrefix())
				.Select(blobRef => Tuple.From(blobRef, _blobProvider.GetBlobOrDelete(blobRef)))
				.Where(pair => pair.Value.HasValue)
				.Select(pair => new CloudServiceInfo
					{
						ServiceName = pair.Key.ServiceName,
						IsStarted = pair.Value.Value == CloudServiceState.Started
					})
				.ToList();
		}

		/// <summary>
		/// Gets info of one cloud service.
		/// </summary>
		public CloudServiceInfo GetService(string serviceName)
		{
			var blob = _blobProvider.GetBlob(new CloudServiceStateReference(serviceName));
			return new CloudServiceInfo
				{
					ServiceName = serviceName,
					IsStarted = blob.Value == CloudServiceState.Started
				};
		}

		/// <summary>
		/// Enumerate the names of all cloud services.
		/// </summary>
		public List<string> GetServiceNames()
		{
			return _blobProvider.List(CloudServiceStateReference.GetPrefix())
				.Select(reference => reference.ServiceName).ToList();
		}

		/// <summary>
		/// Enumerate the names of all user cloud services (system services are skipped).
		/// </summary>
		public List<string> GetUserServiceNames()
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

			return GetServiceNames()
				.Where(service => !systemServices.Contains(service)).ToList();
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
		public void ResetServiceState(string serviceName)
		{
			var blobRef = new CloudServiceStateReference(serviceName);
			_blobProvider.DeleteBlob(blobRef);
		}
	}
}
