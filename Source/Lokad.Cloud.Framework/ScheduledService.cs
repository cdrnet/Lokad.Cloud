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
		/// <summary>Indicates the frequency this service must be called.</summary>
		[DataMember]
		public TimeSpan TriggerInterval { get; set; }

		/// <summary>Date of the last execution.</summary>
		[DataMember]
		public DateTimeOffset LastExecuted { get; set; }

		/// <summary>Indicates whether this service is currently running
		/// (apply only to globally scoped services, not per worker ones).</summary>
		[DataMember(IsRequired = false, EmitDefaultValue = true)]
		public bool IsBusy { get; set; }

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
			var defaultState = GetDefaultState();
			ScheduledServiceState state = null;

			// per-worker scheduling does not need any write request to be made toward the storage
			// storage settings should not over the per-worker aspect of a worker (this setting must
			// be hard-coded)
			if(defaultState.HasValue && defaultState.Value.SchedulePerWorker)
			{
				var blobState = BlobStorage.GetBlob(stateName);
				var now = DateTimeOffset.Now;

				// if no state can be found in the storage, then use default state instead
				state = blobState.HasValue ? blobState.Value : defaultState.Value;

				if(now.Subtract(_lastExecutedOnThisWorker) < state.TriggerInterval)
				{
					_lastExecutedOnThisWorker = now;
					StartOnSchedule();
					return true;
				}

				return false;
			}

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
							newState.Value.IsBusy = true;
							return Result.CreateSuccess(newState.Value);
						}

						// state is a local variable
						state = currentState.Value;

						if (now.Subtract(currentState.Value.LastExecuted) < state.TriggerInterval
								|| state.IsBusy)
						{
							return Result<ScheduledServiceState>.CreateError("No need to update.");
						}

						state.LastExecuted = now;
						state.IsBusy = true;

						return Result.CreateSuccess(state);
					});

			if (!updated)
			{
				return false;
			}

			try
			{
				StartOnSchedule();
				return true;
			}
			finally
			{
				state.IsBusy = false;
				BlobStorage.PutBlob(stateName, state, true);
			}
		}

		/// <summary>
		/// Prepares this service's default state based on its settings attribute.
		/// In case no attribute is found then Maybe.Empty is returned.
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
