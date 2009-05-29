#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Framework
{
	/// <summary>Base class for cloud services.</summary>
	/// <remarks>Do not inherit directly from <see cref="CloudService"/>, inherit from
	/// <see cref="QueueService{T}"/> or <see cref="ScheduledService"/> instead.</remarks>
	public abstract class CloudService
	{
		internal protected ProvidersForCloudService _providers;

		/// <summary>Error logger.</summary>
		public ILog Log
		{
			get { return _providers.Log; }
		}

		/// <summary>Name of the service (used for reporting purposes).</summary>
		/// <remarks>Default implementation returns <c>Type.FullName</c>.</remarks>
		public virtual string Name
		{
			get { return GetType().FullName; }
		}

		protected CloudService(ProvidersForCloudService providers)
		{
			_providers = providers;
		}

		/// <summary>Called when the service is shut down.</summary>
		public virtual void Stop()
		{
			// does nothing
		}

		/// <summary>Put messages into the queue implicitely associated to the
		/// type <c>T</c>.</summary>
		/// <remarks>
		/// The implementation is not constrained by the 8kb limit for <c>T</c> messages.
		/// If messages are larger, the framework will wrap them into the cloud storage.
		/// </remarks>
		public void Put<T>(IEnumerable<T> messages)
		{
			Put(messages, _providers.TypeMapper.GetIdentifier(typeof(T)));
		}

		/// <summary>Put messages into the queue identified by <c>queueId</c>.</summary>
		public void Put<T>(IEnumerable<T> messages, string queueId)
		{
			_providers.QueueStorage.Put(queueId, messages);
		}
	}
}
