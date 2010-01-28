#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using Lokad.Cloud.ServiceFabric;
using Lokad.Quality;
using Lokad.Threading;

namespace Lokad.Cloud
{
	/// <summary>Status flag for <see cref="CloudService"/>s.</summary>
	/// <remarks>Starting / stopping services isn't a synchronous operation,
	/// it can take a little while before all the workers notice an update 
	/// on the service state.</remarks>
	[Serializable]
	public enum CloudServiceState
	{
		/// <summary>
		/// Indicates that the service should be running.</summary>
		Started = 0,

		/// <summary>
		/// Indicates that the service should be stopped.
		/// </summary>
		Stopped = 1
	}

	public class CloudServiceStateName : BaseTypedBlobName<CloudServiceState>
	{
		public override string ContainerName
		{
			get { return CloudService.ServiceStateContainer; }
		}

		[Rank(0)] public readonly string ServiceName;

		public CloudServiceStateName(string serviceName)
		{
			ServiceName = serviceName;
		}

		public static BlobNamePrefix<CloudServiceStateName> GetPrefix()
		{
			return new BlobNamePrefix<CloudServiceStateName>(CloudService.ServiceStateContainer, "");
		}

	}

	/// <summary>Base class for cloud services.</summary>
	/// <remarks>Do not inherit directly from <see cref="CloudService"/>, inherit from
	/// <see cref="QueueService{T}"/> or <see cref="ScheduledService"/> instead.</remarks>
	public abstract class CloudService
	{
		/// <summary>Name of the container associated to temporary items. Each blob
		/// is prefixed with his lifetime expiration date.</summary>
		internal const string TemporaryContainer = "lokad-cloud-temporary";

		internal const string ServiceStateContainer = "lokad-cloud-services-state";
		internal const string DelayedMessageContainer = "lokad-cloud-messages";

		/// <summary>Timeout set at 1h58.</summary>
		/// <remarks>The timeout provided by Windows Azure for message consumption
		/// on queue is set at 2h. Yet, in order to avoid race condition between
		/// message silent re-inclusion in queue and message deletion, the timeout here
		/// is default at 1h58.</remarks>
		protected readonly TimeSpan ExecutionTimeout;

		/// <summary>Indicates the state of the service, as retrieved during the last check.</summary>
		CloudServiceState _state;
		readonly CloudServiceState _defaultState;

		/// <summary>Indicates the last time the service has checked its execution status.</summary>
		DateTimeOffset _lastStateCheck = DateTimeOffset.MinValue;

		/// <summary>Indicates the frequency where the service is actually checking for its state.</summary>
		static TimeSpan StateCheckInterval
		{
			get { return 1.Minutes(); }
		}

		/// <summary>Name of the service (used for reporting purposes).</summary>
		/// <remarks>Default implementation returns <c>Type.FullName</c>.</remarks>
		public virtual string Name
		{
			get { return GetType().FullName; }
		}

		/// <summary>Providers used by the cloud service to access the storage.</summary>
		[UsedImplicitly]
		public CloudInfrastructureProviders Providers { get; set; }

		/// <summary>Short-hand for <c>Providers.BlobStorage</c>.</summary>
		public IBlobStorageProvider BlobStorage { get { return Providers.BlobStorage; } }

		/// <summary>Short-hand for <c>Providers.QueueStorage</c>.</summary>
		public IQueueStorageProvider QueueStorage { get { return Providers.QueueStorage; } }

		/// <summary>Error logger.</summary>
		public ILog Log { get { return Providers.Log; } }

		/// <summary>
		/// Default constructor
		/// </summary>
		protected CloudService()
		{
			// default setting
			_defaultState = CloudServiceState.Started;
			_state = _defaultState;
			ExecutionTimeout = new TimeSpan(1, 58, 0);

			// overwrite settings with config in the attribute - if available
			var settings = GetType().GetAttribute<CloudServiceSettingsAttribute>(true);
			if (null == settings)
			{
				return;
			}

			_defaultState = settings.AutoStart ? CloudServiceState.Started : CloudServiceState.Stopped;
			_state = _defaultState;
			if(settings.ProcessingTimeoutSeconds > 0)
			{
				ExecutionTimeout = TimeSpan.FromSeconds(settings.ProcessingTimeoutSeconds);
			}
		}

		/// <summary>
		/// Wrapper method for the <see cref="StartImpl"/> method. Checks that the
		/// service status before executing the inner start.
		/// </summary>
		/// <returns>
		/// See <seealso cref="StartImpl"/> for the semantic of the return value.
		/// </returns>
		/// <remarks>
		/// If the execution does not complete within 
		/// <see cref="ExecutionTimeout"/>, then a <see cref="TimeoutException"/> is
		/// thrown.
		/// </remarks>
		public ServiceExecutionFeedback Start()
		{
			var now = DateTimeOffset.Now;

			// checking service state at regular interval
			if(now.Subtract(_lastStateCheck) > StateCheckInterval)
			{
				var stateBlobName = new CloudServiceStateName(Name);

				var state = BlobStorage.GetBlobOrDelete(stateBlobName);

				// no state can be retrieved, update blob storage
				if(!state.HasValue)
				{
					state = _defaultState;
					BlobStorage.PutBlob(stateBlobName, state.Value);
				}

				_state = state.Value;
				_lastStateCheck = now;
			}

			// no execution if the service is stopped
			if(CloudServiceState.Stopped == _state)
			{
				return ServiceExecutionFeedback.Skipped;
			}

			var waitFor = new WaitFor<ServiceExecutionFeedback>(ExecutionTimeout);
			return waitFor.Run(StartImpl);
		}

		/// <summary>
		/// Called when the service is launched.
		/// </summary>
		/// <returns>
		/// Feedback with details whether the service did actually perform any
		/// operation, and whether it knows or assumes to have more work available for
		/// immediate execution. This value is used by the framework to adjust the
		/// scheduling behavior for the respective services.
		/// </returns>
		/// <remarks>
		/// This method is expected to be implemented by the framework services not by
		/// the app services.
		/// </remarks>
		protected abstract ServiceExecutionFeedback StartImpl();

		/// <summary>Called when the service is shut down.</summary>
		public virtual void Stop()
		{
			// does nothing
		}

		/// <summary>Put a message into the queue implicitly associated to the type <c>T</c>.</summary>
		public void Put<T>(T message)
		{
			PutRange(new[]{message});
		}

		/// <summary>Put a message into the queue identified by <c>queueName</c>.</summary>
		public void Put<T>(T message, string queueName)
		{
			PutRange(new[] { message }, queueName);
		}

		/// <summary>Put messages into the queue implicitly associated to the type <c>T</c>.</summary>
		public void PutRange<T>(IEnumerable<T> messages)
		{
			PutRange(messages, TypeMapper.GetStorageName(typeof(T)));
		}

		/// <summary>Put messages into the queue identified by <c>queueName</c>.</summary>
		public void PutRange<T>(IEnumerable<T> messages, string queueName)
		{
			QueueStorage.PutRange(queueName, messages);
		}

		/// <summary>Put a message into the queue implicitly associated to the type <c>T</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		public void PutWithDelay<T>(T message, DateTimeOffset triggerTime)
		{
			new DelayedQueue(BlobStorage).PutWithDelay(message, triggerTime);
		}

		/// <summary>Put a message into the queue identified by <c>queueName</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		public void PutWithDelay<T>(T message, DateTimeOffset triggerTime, string queueName)
		{
			new DelayedQueue(BlobStorage).PutWithDelay(message, triggerTime, queueName);
		}

		/// <summary>Put messages into the queue implicitly associated to the type <c>T</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		public void PutRangeWithDelay<T>(IEnumerable<T> messages, DateTimeOffset triggerTime)
		{
			new DelayedQueue(BlobStorage).PutRangeWithDelay(messages, triggerTime);
		}

		/// <summary>Put messages into the queue identified by <c>queueName</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		/// <remarks>This method acts as a delayed put operation, the message not being put
		/// before the <c>triggerTime</c> is reached.</remarks>
		public void PutRangeWithDelay<T>(IEnumerable<T> messages, DateTimeOffset triggerTime, string queueName)
		{
			new DelayedQueue(BlobStorage).PutRangeWithDelay(messages, triggerTime, queueName);
		}
	}
}
