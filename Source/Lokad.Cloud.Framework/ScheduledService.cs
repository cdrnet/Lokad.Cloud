#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Framework
{
	/// <summary>Configuration state of the <seealso cref="ScheduledService"/>.</summary>
	[Serializable]
	public class ScheduledServiceState
	{
		public TimeSpan TriggerInterval { get; set; }
		public DateTime LastExecuted { get; set; }
	}

	/// <summary>This cloud service is automatically called by the framework
	/// on scheduled basis. Scheduling options are provided through the
	/// <see cref="ScheduledServiceSettingsAttribute"/>.</summary>
	public abstract class ScheduledService : CloudService
	{
		public const string ScheduleStateContainer = "lokad-cloud-schedule";
		public const string ScheduleStatePrefix = "state";

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
			// retrieving the state info if any
			var stateName = ScheduleStatePrefix + Delimiter + Name;
			var state = _providers.BlobStorage.GetBlob<ScheduledServiceState>(ScheduleStateContainer, stateName);

			if(!_isInitialized)
			{
				if(null == state)
				{
					var settings = GetType().GetAttribute<ScheduledServiceSettingsAttribute>(true);

					// no trigger interval settings available => we don't execute the service.
					if(settings == null)
					{
						return false;
					}

					// recording a fresh schedule state in the cloud
					_triggerInterval = settings.TriggerInterval.Seconds();

					_providers.BlobStorage.PutBlob(
						ScheduleStateContainer, stateName, 
						new ScheduledServiceState
							{
								LastExecuted = DateTime.MinValue,
								TriggerInterval = _triggerInterval
							});
				}
				else
				{
					_triggerInterval = state.TriggerInterval;
				}

				_isInitialized = true;
			}

			// checking if the last update is not too fresh, and eventually
			// update this value if it's old enough. When the update fails,
			// it simply means that another worker is already on its ways
			// to execute the service.
			var updated = _providers.BlobStorage.UpdateIfNotModified<ScheduledServiceState>(
				ScheduleStateContainer, stateName, currentState =>
					{
						var now = DateTime.Now;

						if(null == currentState)
						{
							return Result.Success(new ScheduledServiceState
								{
                                    TriggerInterval = _triggerInterval,
									LastExecuted = now
								});
						}

						if (now.Subtract(currentState.LastExecuted) < _triggerInterval)
						{
							return Result<ScheduledServiceState>.Error("No need to update.");
						}

						return Result.Success(new ScheduledServiceState
							{
								TriggerInterval = currentState.TriggerInterval,
								LastExecuted = now
							});
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
