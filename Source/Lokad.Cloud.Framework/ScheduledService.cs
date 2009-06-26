#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Framework
{
	/// <summary>This cloud service is automatically called by the framework
	/// on scheduled basis. Scheduling options are provided through the
	/// <see cref="ScheduledServiceSettingsAttribute"/>.</summary>
	public abstract class ScheduledService : CloudService
	{
		public const string ContainerName = "lokad-cloud-schedule";
		public const string LastUpdatedSuffix = "lastupdated";
		public const string TriggerIntervalSuffix = "";
		public const string Delimiter = "/";

		bool _isInitialized;
		TimeSpan _triggerInterval;

		public TimeSpan TriggerInterval
		{
			get
			{
				return _triggerInterval;
			}
			set
			{
				_isInitialized = true;
				_triggerInterval = value;
			}
		}

		/// <summary>IoC constructor.</summary>
		protected ScheduledService(ProvidersForCloudStorage providers) : base(providers)
		{
			// nothing	
		}

		/// <seealso cref="CloudService.StartImpl"/>
		protected sealed override bool StartImpl()
		{
			if(!_isInitialized)
			{
				var triggerIntervalName = Name + Delimiter + TriggerIntervalSuffix;
				_triggerInterval = _providers.BlobStorage.GetBlob<TimeSpan>(ContainerName, triggerIntervalName);

				if(default(TimeSpan) == _triggerInterval)
				{
					var settings = GetType().GetAttribute<ScheduledServiceSettingsAttribute>(true);

					// no trigger interval settings available => we don't execute the service.
					if(settings == null)
					{
						return false;
					}

					// recording trigger interval in the cloud storage
					_triggerInterval = settings.TriggerInterval;
					_providers.BlobStorage.PutBlob(ContainerName, triggerIntervalName, _triggerInterval);
				}

				_isInitialized = true;
			}

			var lastUpdatedName = Name + Delimiter + LastUpdatedSuffix;
			_providers.BlobStorage.GetBlob<DateTime>(ContainerName, lastUpdatedName);

			// checking if the last update is not too fresh, and eventually
			// update this value if it's old enough. When the update fails,
			// it simply means that another worker is already on its ways
			// to execute the service.
			var updated = _providers.BlobStorage.UpdateIfNotModified<DateTime>(ContainerName, lastUpdatedName,
				lastUpdated =>
					{
						var now = DateTime.Now;

						if(now.Subtract(lastUpdated) > _triggerInterval)
						{
							return Result<DateTime>.Error("No need to update.");
						}

						return Result.Success(DateTime.Now);
					});

			if (!updated) return false;
			
			StartOnSchedule();
			return true;
		}

		/// <summary>Called by the framework.</summary>
		/// <remarks>We suggest not performing any heavy processing here. In case
		/// of heavy processing, put a message and use <see cref="QueueService{T}"/>
		/// instead.</remarks>
		protected abstract void StartOnSchedule();
	}
}
