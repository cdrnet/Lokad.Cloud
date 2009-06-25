#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cloud.Framework
{
	/// <summary>Strongly-type queue service (inheritors are instanciated by
	/// reflection on the cloud).</summary>
	/// <typeparam name="T">Message type</typeparam>
	/// <remarks>
	/// <para>The implementation is not constrained by the 8kb limit for <c>T</c> instances.
	/// If the intances are larger, the framework will wrap them into the cloud storage.</para>
	/// <para>Whenever possible, we suggest to design the service logic to be idempotent
	/// in order to make the service reliable and ultimately consistent.</para>
	/// </remarks>
	public abstract class QueueService<T> : CloudService
	{
		readonly string _queueName;
		readonly int _batchSize;

		/// <summary>Name of the queue associated to the service.</summary>
		public override string Name
		{
			get { return _queueName; }
		}

		/// <summary>IoC constructor.</summary>
		protected QueueService(ProvidersForCloudStorage providers)
			: base(providers)
		{
			var settings = GetType().GetAttribute<QueueServiceSettingsAttribute>(true);

			if(null != settings) // settings are provided through custom attribute
			{
				_queueName = settings.QueueName;
				_batchSize = Math.Max(settings.BatchSize, 1); // need to be at least 1
			}
			else // default setting
			{
				_queueName = _providers.TypeMapper.GetStorageName(typeof (T));
				_batchSize = 1;
			}
		}

		/// <summary>Do not override this method, use <see cref="Start(IEnumerable{T})"/>
		/// instead.</summary>
		protected override bool StartImpl()
		{
			var messages = _providers.QueueStorage.Get<T>(_queueName, _batchSize);

			var count = messages.Count();
			if (count > 0) Start(messages);

			return count > 0;
		}

		/// <summary>Method called by the <c>Lokad.Cloud</c> framework when messages are
		/// available for processing.</summary>
		/// <param name="messages">Messages to be processed.</param>
		/// <remarks>
		/// We suggest to make messages deleted asap through the <see cref="Delete"/>
		/// method. Otherwise, messages will be automatically deleted when the method
		/// returns (except if an exception is thrown obviously).
		/// </remarks>
		protected abstract void Start(IEnumerable<T> messages);

		/// <summary>Get more messages from the underlying queue.</summary>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <returns>Retrieved messages (enumeration might be empty).</returns>
		/// <remarks>It is suggested to <see cref="Delete"/> messages first
		/// before asking for more.</remarks>
		public IEnumerable<T> GetMore(int count)
		{
			return _providers.QueueStorage.Get<T>(_queueName, count);
		}

		/// <summary>Get more message from an arbitrary queue.</summary>
		/// <typeparam name="U">Message type.</typeparam>
		/// <param name="count">Number of message to be retrieved.</param>
		/// <param name="queueName">Name of the queue.</param>
		/// <returns>Retrieved message (enumeration might be empty).</returns>
		public IEnumerable<U> GetMore<U>(int count, string queueName)
		{
			return _providers.QueueStorage.Get<U>(_queueName, count);
		}

		/// <seealso cref="Delete(IEnumerable{U})"/>
		public void Delete<U>(U message)
		{
			Delete(new[]{message});
		}

		/// <summary>Delete messages retrieved either through <see cref="StartImpl"/>
		/// or through <see cref="GetMore"/>.</summary>
		public void Delete<U>(IEnumerable<U> messages)
		{
			_providers.QueueStorage.Delete(_queueName, messages);
		}
	}
}
