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
		/// <summary>Gets the list of queues whose name start with the specifed prefix.</summary>
		/// <param name="prefix">If null or empty, returns all queues.</param>
		IEnumerable<string> List(string prefix);

		/// <summary>Gets messages from a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue to be pulled.</param>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <returns>Enumeration of messages, possibly empty.</returns>
		IEnumerable<T> Get<T>(string queueName, int count);

		/// <summary>Put a message on a queue.</summary>
		void Put<T>(string queueName, T message);

		/// <summary>Put messages on a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue where messages are put.</param>
		/// <param name="messages">Messages to be put.</param>
		/// <remarks>If the queue does not exist, it gets created.</remarks>
		void PutRange<T>(string queueName, IEnumerable<T> messages);

		/// <summary>Clear all the messages from the specified queue.</summary>
		void Clear(string queueName);

		/// <summary>Deletes a message from a queue.</summary>
		/// <returns><c>True</c> if the message has been deleted.</returns>
		bool Delete<T>(string queueName, T message);

		/// <summary>Deletes messages from a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue where the messages are removed from.</param>
		/// <param name="messages">Messages to be removed.</param>
		/// <returns>The number of messages actually deleted.</returns>
		/// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
		int DeleteRange<T>(string queueName, IEnumerable<T> messages);

		/// <summary>Deletes a queue.</summary>
		/// <remarks><c>true</c> if the queue name has been actually deleted.</remarks>
		bool DeleteQueue(string queueName);

		/// <summary>Gets the approximate number of items in this queue.</summary>
		int GetApproximateCount(string queueName);
	}
}
