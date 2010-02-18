#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud
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
		/// <summary>Gets the list of queues whose name start with the specified prefix.</summary>
		/// <param name="prefix">If <c>null</c> or empty, returns all queues.</param>
		IEnumerable<string> List(string prefix);

		/// <summary>Gets messages from a queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="queueName">Identifier of the queue to be pulled.</param>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <param name="visibilityTimeout">
		/// The visibility timeout, indicating when the not yet deleted message should
		/// become visible in the queue again.
		/// </param>
		/// <returns>Enumeration of messages, possibly empty.</returns>
		IEnumerable<T> Get<T>(string queueName, int count, TimeSpan visibilityTimeout);

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

		/// <summary>Deletes a message being processed from the queue.</summary>
		/// <returns><c>True</c> if the message has been deleted.</returns>
		/// <remarks>Message must have first been retrieved through <see cref="Get{T}"/>.</remarks>
		bool Delete<T>(T message);

		/// <summary>Deletes messages being processed from the queue.</summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="messages">Messages to be removed.</param>
		/// <returns>The number of messages actually deleted.</returns>
		/// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
		int DeleteRange<T>(IEnumerable<T> messages);

		/// <summary>
		/// Abandon a message being processed and put it visibly back on the queue.
		/// </summary>
		/// <typeparam name="T">Type of the message.</typeparam>
		/// <param name="message">Message to be abandoned.</param>
		/// <returns><c>True</c> if the original message has been deleted.</returns>
		/// <remarks>Message must have first been retrieved through <see cref="Get{T}"/>.</remarks>
		bool Abandon<T>(T message);

		/// <summary>
		/// Abandon a set of messages being processed and put them visibly back on the queue.
		/// </summary>
		/// <typeparam name="T">Type of the messages.</typeparam>
		/// <param name="messages">Messages to be abandoned.</param>
		/// <returns>The number of original messages actually deleted.</returns>
		/// <remarks>Messages must have first been retrieved through <see cref="Get{T}"/>.</remarks>
		int AbandonRange<T>(IEnumerable<T> messages);

		/// <summary>Deletes a queue.</summary>
		/// <returns><c>true</c> if the queue name has been actually deleted.</returns>
		bool DeleteQueue(string queueName);

		/// <summary>Gets the approximate number of items in this queue.</summary>
		int GetApproximateCount(string queueName);
	}
}
