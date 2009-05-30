#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Framework
{
	/// <summary>This cloud service is automatically called by the framework
	/// on scheduled basis. Scheduling options are provided through the
	/// <see cref="ScheduledServiceSettingsAttribute"/>.</summary>
	public abstract class ScheduledService : CloudService
	{
		/// <summary>IoC constructor.</summary>
		protected ScheduledService(ProvidersForCloudStorage providers) : base(providers)
		{
			// nothing	
		}

		/// <seealso cref="CloudService.Start"/>
		public override bool Start()
		{
			// TODO: must check here that the activation time is right.
			throw new System.NotImplementedException();
		}

		/// <summary>Called by the framework.</summary>
		/// <remarks>We suggest not performing any heavy processing here. In case
		/// of heavy processing, put a message and use <see cref="QueueService{T}"/>
		/// instead.</remarks>
		public abstract void StartOnSchedule();
	}
}
