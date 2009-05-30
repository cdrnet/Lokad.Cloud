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

		/// <summary>IoC constructor.</summary>
		protected QueueService(ProvidersForCloudService providers)
			: base(providers)
		{
			var settings = GetType().GetAttribute<QueueServiceSettingsAttribute>(true);

			if(null != settings) // settings are provided through custom attribute
			{
				_queueName = settings.QueueName;
				_batchSize = settings.BatchSize;
			}
			else // default setting
			{
				_queueName = _providers.TypeMapper.GetStorageName(typeof (T));
				_batchSize = 1;
			}
		}

		/// <summary>Do not override this method, use <see cref="Start(IEnumerable{T})"/>
		/// instead.</summary>
		public override bool Start()
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
		public abstract void Start(IEnumerable<T> messages);

		/// <summary>Get more messages from the underlying queue.</summary>
		/// <param name="count">Maximal number of messages to be retrieved.</param>
		/// <returns>Retrieved messages (enumeration might be empty).</returns>
		/// <remarks>It is suggested to <see cref="Delete"/> messages first
		/// before asking for more.</remarks>
		public IEnumerable<T> GetMore(int count)
		{
			throw new NotImplementedException();
		}

		/// <summary>Delete messages retrieved either through <see cref="Start"/>
		/// or through <see cref="GetMore"/>.</summary>
		public void Delete(IEnumerable<T> messages)
		{
			throw new NotImplementedException();
		}
	}
}
