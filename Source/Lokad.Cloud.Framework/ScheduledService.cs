#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud
{
	/// <summary>Configuration state of the <seealso cref="ScheduledService"/>.</summary>
	[Serializable]
	public class ScheduledServiceState
	{
		public TimeSpan TriggerInterval { get; set; }
		public DateTime LastExecuted { get; set; }
	}

	public class ScheduledServiceStateName : BaseTypedBlobName<ScheduledServiceState>
	{

		public override string ContainerName
		{
			get { return ScheduledService.ScheduleStateContainer; }
		}

		[Pos(0)] public readonly string ServiceName;

		public ScheduledServiceStateName(string serviceName)
		{
			ServiceName = serviceName;
		}

		public static BlobNamePrefix<ScheduledServiceStateName> GetPrefix()
		{
			return new BlobNamePrefix<ScheduledServiceStateName>(ScheduledService.ScheduleStateContainer, "");
		}

	}

	/// <summary>This cloud service is automatically called by the framework
	/// on scheduled basis. Scheduling options are provided through the
	/// <see cref="ScheduledServiceSettingsAttribute"/>.</summary>
	/// <remarks>A empty constructor is needed for instantiation through reflection.</remarks>
	public abstract class ScheduledService : CloudService
	{
		internal const string ScheduleStateContainer = "lokad-cloud-schedule-state";

		bool _isInitialized;
		TimeSpan _triggerInterval;

		/// <seealso cref="CloudService.StartImpl"/>
		protected sealed override bool StartImpl()
		{
			// retrieving the state info if any
			var stateName = new ScheduledServiceStateName(Name);
			var state = BlobStorage.GetBlob<ScheduledServiceState>(stateName);

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

					var writeSucceeded = BlobStorage.PutBlob(stateName, 
						new ScheduledServiceState
							{
								LastExecuted = DateTime.MinValue,
								TriggerInterval = _triggerInterval
							}, false);

					// if write fails, another worker is concurrently executing
					if(!writeSucceeded) return false;
				}
				else
				{
					_triggerInterval = state.TriggerInterval;
				}

				_isInitialized = true;
			}

			// checking if the last update is not too recent, and eventually
			// update this value if it's old enough. When the update fails,
			// it simply means that another worker is already on its ways
			// to execute the service.
			var updated = BlobStorage.UpdateIfNotModified<ScheduledServiceState>(stateName, 
				currentState =>
					{
						var now = DateTime.UtcNow;

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
							return Result<ScheduledServiceState>.CreateError("No need to update.");
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
