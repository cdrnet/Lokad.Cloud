#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cloud.Storage.Queues
{
    /// <summary>Helper extensions methods for storage providers.</summary>
    public static class QueueStorageExtensions
    {
        /// <summary>Gets messages from a queue with a visibility timeout of 2 hours and a maximum of 50 processing trials.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue to be pulled.</param>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, string queueName, int count)
        {
            return provider.Get<T>(queueName, count, new TimeSpan(2, 0, 0), 5);
        }

        /// <summary>Gets messages from a queue with a visibility timeout of 2 hours.</summary>
        /// <typeparam name="T">Type of the messages.</typeparam>
        /// <param name="queueName">Identifier of the queue to be pulled.</param>
        /// <param name="count">Maximal number of messages to be retrieved.</param>
        /// <param name="maxProcessingTrials">
        /// Maximum number of message processing trials, before the message is considered as
        /// being poisonous, removed from the queue and persisted to the 'failing-messages' store.
        /// </param>
        /// <returns>Enumeration of messages, possibly empty.</returns>
        public static IEnumerable<T> Get<T>(this IQueueStorageProvider provider, string queueName, int count, int maxProcessingTrials)
        {
            return provider.Get<T>(queueName, count, new TimeSpan(2, 0, 0), maxProcessingTrials);
        }
    }
}
