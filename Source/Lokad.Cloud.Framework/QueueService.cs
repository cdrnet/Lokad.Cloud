#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud
{
	/// <summary>Strongly-type queue service (inheritors are instantiated by
	/// reflection on the cloud).</summary>
	/// <typeparam name="T">Message type</typeparam>
	/// <remarks>
	/// <para>The implementation is not constrained by the 8kb limit for <c>T</c> instances.
	/// If the instances are larger, the framework will wrap them into the cloud storage.</para>
	/// <para>Whenever possible, we suggest to design the service logic to be idempotent
	/// in order to make the service reliable and ultimately consistent.</para>
	/// <para>A empty constructor is needed for instantiation through reflection.</para>
	/// </remarks>
	public abstract class QueueService<T> : CloudService
	{
		readonly string _queueName;
		readonly int _batchSize;
		readonly TimeSpan _visibilityTimeout;

		/// <summary>Name of the queue associated to the service.</summary>
		public override string Name
		{
			get { return _queueName; }
		}

		/// <summary>Default constructor</summary>
		protected QueueService()
		{
			var settings = GetType().GetAttribute<QueueServiceSettingsAttribute>(true);

			if(null != settings) // settings are provided through custom attribute
			{
				_queueName = settings.QueueName ?? TypeMapper.GetStorageName(typeof(T));
				_batchSize = Math.Max(settings.BatchSize, 1); // need to be at least 1
			}
			else // default setting
			{
				_queueName = TypeMapper.GetStorageName(typeof(T));
				_batchSize = 1;
			}

			// 1.25 * execution timeout, but limited to 2h max
			_visibilityTimeout = TimeSpan.FromSeconds(Math.Max(1, Math.Min(7200, (1.25*ExecutionTimeout.TotalSeconds))));
		}

		/// <summary>Do not try to override this method, use <see cref="StartRange"/>
		/// instead.</summary>
		protected sealed override ServiceExecutionFeedback StartImpl()
		{
			var messages = QueueStorage.Get<T>(_queueName, _batchSize, _visibilityTimeout);

			var count = messages.Count();
			if (count > 0)
			{
				StartRange(messages);
			}

			// Messages might have already been deleted by the 'Start' method.
			// It's OK, 'Delete' is idempotent.
			DeleteRange(messages);

			return count > 0
				? ServiceExecutionFeedback.WorkAvailable
				: ServiceExecutionFeedback.Skipped;
		}

		/// <summary>Method called first by the <c>Lokad.Cloud</c> framework when messages are
		/// available for processing. Default implementation is naively calling <see cref="Start"/>.
		/// </summary>
		/// <param name="messages">Messages to be processed.</param>
		/// <remarks>
		/// We suggest to make messages deleted asap through the <see cref="DeleteRange"/>
		/// method. Otherwise, messages will be automatically deleted when the method
		/// returns (except if an exception is thrown obviously).
		/// </remarks>
		protected virtual void StartRange(IEnumerable<T> messages)
		{
			foreach(var message in messages)
			{
				Start(message);
			}
		}

		/// <summary>Method called by <see cref="StartRange"/>, passing the message.</summary>
		/// <remarks>
		/// This method is a syntactic sugar for <see cref="QueueService{T}"/> inheritors
		/// dealing only with 1 message at a time.
		/// </remarks>
		protected virtual void Start(T message)
		{
			throw new NotSupportedException("Start or StartRange method must overridden by inheritor.");
		}

		/// <summary>Get more messages from the underlying queue.</summary>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <returns>Retrieved messages (enumeration might be empty).</returns>
		/// <remarks>It is suggested to <see cref="DeleteRange"/> messages first
		/// before asking for more.</remarks>
		public IEnumerable<T> GetMore(int count)
		{
			return QueueStorage.Get<T>(_queueName, count, _visibilityTimeout);
		}

		/// <summary>Get more message from an arbitrary queue.</summary>
		/// <param name="count">Number of message to be retrieved.</param>
		/// <param name="queueName">Name of the queue.</param>
		/// <returns>Retrieved message (enumeration might be empty).</returns>
		public IEnumerable<T> GetMore(int count, string queueName)
		{
			return QueueStorage.Get<T>(queueName, count, _visibilityTimeout);
		}

		/// <summary>Delete message retrieved either through <see cref="StartRange"/>
		/// or through <see cref="GetMore(int)"/>.</summary>
		public void Delete(T message)
		{
			QueueStorage.Delete(_queueName, message);
		}

		/// <summary>Delete messages retrieved either through <see cref="StartRange"/>
		/// or through <see cref="GetMore(int)"/>.</summary>
		public void DeleteRange(IEnumerable<T> messages)
		{
			QueueStorage.DeleteRange(_queueName, messages);
		}
	}
}
