#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Framework
{
	/// <summary>Default settings for the <see cref="QueueService{T}"/>. Once the queue
	/// service is deployed, settings are stored in the <c>lokad-cloud-queues</c> blob
	/// container.</summary>
	sealed class QueueServiceSettingsAttribute : CloudServiceSettingsAttribute
	{
		/// <summary>Name of the queue attached to the <see cref="QueueService{T}"/>.</summary>
		/// <remarks>If this value is null or empty, a default queue name is chosen based
		/// on the type <c>T</c>.</remarks>
		public string QueueName { get; set; }

		/// <summary>Suggested size for batch retrieval of messages.</summary>
		/// <remarks>The maximal value is 1000. We suggest to retrieve small messages
		/// in batch to reduce network overhead.</remarks>
		public int BatchSize { get; set; }
	}
}
