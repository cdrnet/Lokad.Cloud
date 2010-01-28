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

	public class ScheduledServiceStateReference : BlobReference<ScheduledServiceState>
	{
		public override string ContainerName
		{
			get { return ScheduledService.ScheduleStateContainer; }
		}

		[Rank(0)] public readonly string ServiceName;

		public ScheduledServiceStateReference(string serviceName)
		{
			ServiceName = serviceName;
		}

		public static BlobNamePrefix<ScheduledServiceStateReference> GetPrefix()
		{
			return new BlobNamePrefix<ScheduledServiceStateReference>(ScheduledService.ScheduleStateContainer, "");
		}
	}

	/// <summary>This cloud service is automatically called by the framework
	/// on scheduled basis. Scheduling options are provided through the
	/// <see cref="ScheduledServiceSettingsAttribute"/>.</summary>
	/// <remarks>A empty constructor is needed for instantiation through reflection.</remarks>
	public abstract class ScheduledService : CloudService
	{
		internal const string ScheduleStateContainer = "lokad-cloud-schedule-state";

		readonly bool _scheduledPerWorker;
		readonly string _workerKey;
		readonly TimeSpan _leaseTimeout;
		readonly TimeSpan _defaultTriggerPeriod;

		DateTimeOffset _workerScopeLastExecuted;

		/// <summary>
		/// Constructor
		/// </summary>
		protected ScheduledService()
		{
			// runtime fixed settings
			_leaseTimeout = ExecutionTimeout + 5.Minutes();
			_workerKey = CloudEnvironment.PartitionKey;

			// default setting
			_scheduledPerWorker = false;
			_defaultTriggerPeriod = 1.Hours();

			// overwrite settings with config in the attribute - if available
			var settings = GetType().GetAttribute<ScheduledServiceSettingsAttribute>(true);
			if (settings != null)
			{
				_scheduledPerWorker = settings.SchedulePerWorker;

				if (settings.TriggerInterval > 0)
				{
					_defaultTriggerPeriod = settings.TriggerInterval.Seconds();
				}
			}
		}

		/// <seealso cref="CloudService.StartImpl"/>
		protected sealed override ServiceExecutionFeedback StartImpl()
		{
			var stateReference = new ScheduledServiceStateReference(Name);

			// 1. SIMPLE WORKER-SCOPED SCHEDULING CASE

			// per-worker scheduling does not need any write request to be made toward the storage
			if (_scheduledPerWorker)
			{
				// if no state can be found in the storage, then use default state instead
				var blobState = BlobStorage.GetBlob(stateReference);
				var state = blobState.HasValue ? blobState.Value : GetDefaultState();

				var now = DateTimeOffset.Now;
				if (now.Subtract(_workerScopeLastExecuted) < state.TriggerInterval)
				{
					_workerScopeLastExecuted = now;
					StartOnSchedule();
					return ServiceExecutionFeedback.DoneForNow;
				}

				return ServiceExecutionFeedback.Skipped;
			}

			// 2. CHECK WHETHER WE SHOULD EXECUTE NOW, ACQUIRE LEASE IF SO

			// checking if the last update is not too recent, and eventually
			// update this value if it's old enough. When the update fails,
			// it simply means that another worker is already on its ways
			// to execute the service.
			var updated = BlobStorage.UpdateIfNotModified(
				stateReference,
				currentState =>
					{
						var now = DateTimeOffset.Now;

						if (!currentState.HasValue)
						{
							// initialize default state
							var newState = GetDefaultState();

							// create lease and execute
							newState.LastExecuted = now;
							newState.Lease = CreateLease(now);
							return Result.CreateSuccess(newState);
						}

						var state = currentState.Value;
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

			// 3. IF WE SHOULD NOT EXECUTE NOW, SKIP

			if (!updated)
			{
				return ServiceExecutionFeedback.Skipped;
			}

			try
			{
				// 4. ACTUAL EXECUTION

				StartOnSchedule();
				return ServiceExecutionFeedback.DoneForNow;
			}
			finally
			{
				// 5. RELEASE THE LEASE

				// we need a full update here (instead of just uploading the cached blob)
				// to ensure we do not overwrite changes made in the console in the meantime
				// (e.g. changed trigger interval), and to resolve the edge case when
				// a lease has been forcefully removed from the console and another service
				// has taken a lease in the meantime.

				ScheduledServiceState result;
				BlobStorage.AtomicUpdate(
					stateReference,
					currentState =>
						{
							if (!currentState.HasValue)
							{
								return GetDefaultState();
							}

							var state = currentState.Value;
							if (state.Lease == null || state.Lease.Owner != _workerKey)
							{
								return state;
							}

							// remove lease
							state.Lease = null;
							return state;
						},
					out result);
			}
		}

		/// <summary>
		/// Prepares this service's default state based on its settings attribute.
		/// In case no attribute is found then Maybe.Empty is returned.
		/// </summary>
		private ScheduledServiceState GetDefaultState()
		{
			return new ScheduledServiceState
			{
				LastExecuted = DateTimeOffset.MinValue,
				TriggerInterval = _defaultTriggerPeriod,
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
