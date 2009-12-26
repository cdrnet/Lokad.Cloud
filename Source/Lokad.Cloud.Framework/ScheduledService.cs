#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;

namespace Lokad.Cloud
{
	/// <summary>Configuration state of the <seealso cref="ScheduledService"/>.</summary>
	[Serializable, DataContract]
	public class ScheduledServiceState
	{
		[DataMember]
		public TimeSpan TriggerInterval { get; set; }

		[DataMember]
		public DateTimeOffset LastExecuted { get; set; }

		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public bool SchedulePerWorker { get; set; }
	}

	public class ScheduledServiceStateName : BaseTypedBlobName<ScheduledServiceState>
	{
		public override string ContainerName
		{
			get { return ScheduledService.ScheduleStateContainer; }
		}

		[Rank(0)] public readonly string ServiceName;

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

		DateTimeOffset _lastExecutedOnThisWorker;

		/// <seealso cref="CloudService.StartImpl"/>
		protected sealed override bool StartImpl()
		{
			var stateName = new ScheduledServiceStateName(Name);

			// checking if the last update is not too recent, and eventually
			// update this value if it's old enough. When the update fails,
			// it simply means that another worker is already on its ways
			// to execute the service.
			var updated = BlobStorage.UpdateIfNotModified(
				stateName,
				currentState =>
					{
						var now = DateTimeOffset.Now;

						if (!currentState.HasValue)
						{
							// Initialize State
							var newState = GetDefaultState();
							if (!newState.HasValue)
							{
								return Result<ScheduledServiceState>.CreateError("No settings available, service skipped.");
							}

							_lastExecutedOnThisWorker = now;
							newState.Value.LastExecuted = now;
							return Result.CreateSuccess(newState.Value);
						}

						var state = currentState.Value;

						if (state.SchedulePerWorker && now.Subtract(_lastExecutedOnThisWorker) < state.TriggerInterval
							|| !state.SchedulePerWorker && now.Subtract(currentState.Value.LastExecuted) < state.TriggerInterval)
						{
							return Result<ScheduledServiceState>.CreateError("No need to update.");
						}

						_lastExecutedOnThisWorker = now;
						state.LastExecuted = now;
						return Result.CreateSuccess(state);
					});

			if (!updated)
			{
				return false;
			}
			
			StartOnSchedule();
			return true;
		}

		/// <summary>
		/// Prepares this service's default state based on its settings attribute.
		/// If case no attribute is found then Maybe.Empty is returned.
		/// </summary>
		private Maybe<ScheduledServiceState> GetDefaultState()
		{
			var settings = GetType().GetAttribute<ScheduledServiceSettingsAttribute>(true);

			// no trigger interval settings available => we don't execute the service.
			if (settings == null)
			{
				return Maybe<ScheduledServiceState>.Empty;
			}

			return new ScheduledServiceState
				{
					LastExecuted = DateTimeOffset.MinValue,
					TriggerInterval = settings.TriggerInterval.Seconds(),
					SchedulePerWorker = settings.SchedulePerWorker
				};
		}

		/// <summary>Called by the framework.</summary>
		/// <remarks>We suggest not performing any heavy processing here. In case
		/// of heavy processing, put a message and use <see cref="QueueService{T}"/>
		/// instead.</remarks>
		protected abstract void StartOnSchedule();
	}
}
