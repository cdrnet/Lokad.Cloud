#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Lokad.Cloud.ServiceFabric;
using Lokad.Quality;

namespace Lokad.Cloud.Storage
{
	/// <summary>Used as a wrapper for delayed messages (stored in the
	/// blob storage waiting to be pushed into a queue).</summary>
	/// <seealso cref="DelayedQueue.PutWithDelay{T}(T,System.DateTime)"/>
	/// <remarks>
	/// Due to genericity, this message is not tagged with <c>DataContract</c>.
	/// </remarks>
	[Serializable]
	class DelayedMessage
	{
		/// <summary>Name of the queue where the inner message will be put
		/// once the delay is expired.</summary>
		public string QueueName { get; set; }

		/// <summary>Inner message.</summary>
		public object InnerMessage { get; set; }

		/// <summary>Full constructor.</summary>
		public DelayedMessage(string queueName, object innerMessage)
		{
			QueueName = queueName;
			InnerMessage = innerMessage;
		}
	}

	[Serializable, DataContract]
	class DelayedMessageName : BlobName<DelayedMessage>
	{
		public override string ContainerName
		{
			get { return CloudService.DelayedMessageContainer; }
		}

		[Rank(0, true), DataMember] public readonly DateTimeOffset TriggerTime;
		[UsedImplicitly, Rank(1, true), DataMember] public readonly Guid Identifier;

        /// <summary>Empty constructor, used for prefixing.</summary>
        public DelayedMessageName()
        {
        }

		public DelayedMessageName(DateTimeOffset triggerTime, Guid identifier)
		{
			TriggerTime = triggerTime;
			Identifier = identifier;
		}
	}

	/// <summary>Allows to put messages in a queue, delaying them as needed.</summary>
	/// <remarks>A <see cref="IBlobStorageProvider"/> is used for storing messages that 
	/// must be enqueued with a delay.</remarks>
	[Immutable]
	public class DelayedQueue
	{
		readonly IBlobStorageProvider _provider;

		/// <summary>Initializes a new instance of the <see cref="DelayedQueue"/> class.</summary>
		/// <param name="provider">The blob storage provider.</param>
		public DelayedQueue(IBlobStorageProvider provider)
		{
			Enforce.Argument(() => provider);
			_provider = provider;
		}

		/// <summary>Put a message into the queue implicitly associated to the type <c>T</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		public void PutWithDelay<T>(T message, DateTimeOffset triggerTime)
		{
			PutWithDelay(message, triggerTime, TypeMapper.GetStorageName(typeof(T)));
		}

		/// <summary>Put a message into the queue identified by <c>queueName</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		public void PutWithDelay<T>(T message, DateTimeOffset triggerTime, string queueName)
		{
			PutRangeWithDelay(new[] { message }, triggerTime, queueName);
		}

		/// <summary>Put messages into the queue implicitly associated to the type <c>T</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		public void PutRangeWithDelay<T>(IEnumerable<T> messages, DateTimeOffset triggerTime)
		{
			PutRangeWithDelay(messages, triggerTime, TypeMapper.GetStorageName(typeof(T)));
		}

		/// <summary>Put messages into the queue identified by <c>queueName</c> at the
		/// time specified by the <c>triggerTime</c>.</summary>
		/// <remarks>This method acts as a delayed put operation, the message not being put
		/// before the <c>triggerTime</c> is reached.</remarks>
		public void PutRangeWithDelay<T>(IEnumerable<T> messages, DateTimeOffset triggerTime, string queueName)
		{
			foreach(var message in messages)
			{
				var blobRef = new DelayedMessageName(triggerTime, Guid.NewGuid());
				var envelope = new DelayedMessage(queueName, message);
				_provider.PutBlob(blobRef, envelope);
			}
		}

	}
}