#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using Lokad.Cloud.Azure;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud
{
	/// <summary>Configuration state of the <seealso cref="ScheduledService"/>.</summary>
	[Serializable, DataContract]
	public class ScheduledServiceState
	{
		/// <summary>
		/// Indicates the frequency this service must be called.
		/// </summary>
		[DataMember]
		public TimeSpan TriggerInterval { get; set; }

		/// <summary>
		/// Date of the last execution.
		/// </summary>
		[DataMember]
		public DateTimeOffset LastExecuted { get; set; }

		/// <summary>
		/// Lease state info to support synchronized exclusive execution of this
		/// service (applies only to cloud scoped service, not per worker scheduled
		/// ones). If <c>null</c> then the service is not currently leased by any
		/// worker.
		/// </summary>
		[DataMember(IsRequired = false, EmitDefaultValue = false)]
		public SynchronizationLeaseState Lease { get; set; }

		/// <summary>
		/// Indicates whether this service is currently running
		/// (apply only to globally scoped services, not per worker ones)
		/// .</summary>
		[Obsolete("Use the Lease mechanism instead.")]
		[DataMember(IsRequired = false, EmitDefaultValue = false)]
		public bool IsBusy { get; set; }

		//[Obsolete("Scheduling scope is fixed at compilation time and thus not a state of the service.")]
		[DataMember(IsRequired = false, EmitDefaultValue = false)]
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
		readonly bool _scheduledPerWorker;
		readonly TimeSpan _leaseTimeout;
		readonly Maybe<TimeSpan> _defaultTriggerPeriod;
		readonly string _workerKey;

		/// <summary>
		/// Constructor
		/// </summary>
		protected ScheduledService()
		{
			_leaseTimeout = ExecutionTimeout + new TimeSpan(0, 5, 0);

			var settings = GetType().GetAttribute<ScheduledServiceSettingsAttribute>(true);
			if (settings == null)
			{
				_scheduledPerWorker = false;
				_defaultTriggerPeriod = Maybe<TimeSpan>.Empty;
				return;
			}

			_scheduledPerWorker = settings.SchedulePerWorker;
			_defaultTriggerPeriod = settings.TriggerInterval.Seconds();
			_workerKey = CloudEnvironment.PartitionKey;
		}

		/// <seealso cref="CloudService.StartImpl"/>
		protected sealed override bool StartImpl()
		{
			var stateName = new ScheduledServiceStateName(Name);
			ScheduledServiceState state = null;

			// per-worker scheduling does not need any write request to be made toward the storage
			if (_scheduledPerWorker)
			{
				// if no state can be found in the storage, then use default state instead
				var blobState = BlobStorage.GetBlob(stateName);
				var stateIfAvailable = blobState.HasValue ? blobState.Value : GetDefaultState();

				// skip execution if no trigger information is available.
				if (!stateIfAvailable.HasValue)
				{
					return false;
				}

				var now = DateTimeOffset.Now;
				if (now.Subtract(_lastExecutedOnThisWorker) < stateIfAvailable.Value.TriggerInterval)
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

							// state is a local variable
							state = newState.Value; 

							// create lease and execute
							state.LastExecuted = now;
							state.Lease = CreateLease(now);
							return Result.CreateSuccess(state);
						}

						// state is a local variable
						state = currentState.Value;

						if (now.Subtract(state.LastExecuted) < state.TriggerInterval)
						{
							return Result<ScheduledServiceState>.CreateError("No need to update.");
						}

						if (state.Lease != null && state.Lease.Timeout > now)
						{
							return Result<ScheduledServiceState>.CreateError("Update needed but blocked by lease.");
						}

						// create lease and execute
						state.LastExecuted = now;
						state.Lease = CreateLease(now);
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
				state.Lease = null;
				BlobStorage.PutBlob(stateName, state, true);
			}
		}

		/// <summary>
		/// Prepares this service's default state based on its settings attribute.
		/// In case no attribute is found then Maybe.Empty is returned.
		/// </summary>
		private Maybe<ScheduledServiceState> GetDefaultState()
		{
			// skip execution if no trigger information is available.
			if(!_defaultTriggerPeriod.HasValue)
			{
				return Maybe<ScheduledServiceState>.Empty;
			}

			return new ScheduledServiceState
				{
					LastExecuted = DateTimeOffset.MinValue,
					TriggerInterval = _defaultTriggerPeriod.Value,
					SchedulePerWorker = _scheduledPerWorker
				};
		}

		/// <summary>
		/// Prepares a new lease.
		/// </summary>
		private SynchronizationLeaseState CreateLease(DateTimeOffset now)
		{
			return new SynchronizationLeaseState
				{
					Acquired = now,
					Timeout = now + _leaseTimeout,
					Owner = _workerKey
				};
		}

		/// <summary>Called by the framework.</summary>
		/// <remarks>We suggest not performing any heavy processing here. In case
		/// of heavy processing, put a message and use <see cref="QueueService{T}"/>
		/// instead.</remarks>
		protected abstract void StartOnSchedule();
	}
}
