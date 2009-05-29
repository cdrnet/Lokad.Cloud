#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System.Collections.Generic;

namespace Lokad.Cloud.Core
{
	/// <summary>Abstraction of the Queue Storage.</summary>
	/// <remarks>
	/// This provider represents a <em>logical</em> queue, not the actual
	/// Queue Storage. In particular, the provider implementation deals
	/// with overflowing messages (that is to say messages larger than 8kb)
	/// on its own.
	/// </remarks>
	public interface IQueueStorageProvider
	{
		/// <summary>Gets messages from a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue to be pulled.</param>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <returns>Enumeration of messages, possibly empty.</returns>
		IEnumerable<T> Get<T>(string queueName, int count);

		/// <summary>Put message on a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue where messages are put.</param>
		/// <param name="messages">Messages to be put.</param>
		void Put<T>(string queueName, IEnumerable<T> messages);

		/// <summary>Deletes messages from a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue where the messages are removed from.</param>
		/// <param name="messages">Messages to be removed.</param>
		/// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
		void Delete<T>(string queueName, IEnumerable<T> messages);
	}
}
