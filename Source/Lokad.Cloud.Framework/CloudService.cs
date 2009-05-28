#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Cloud.Framework
{
	/// <summary>Base class for cloud services.</summary>
	public abstract class CloudService
	{
		/// <summary>Name of the service (used for reporting purposes).</summary>
		public abstract string Name { get; }

		/// <summary>Called when the service is shut down.</summary>
		public virtual void Stop()
		{
			// does nothing
		}

		/// <summary>Put messages into the queue implicitely associated to the
		/// type <c>T</c>.</summary>
		/// <remarks>
		/// The implementation is not constrained by the 8kb limit for <c>T</c> messages.
		/// If messages are larger, the framework will wrap them into the cloud storage.
		/// </remarks>
		public void Put<T>(IEnumerable<T> messages)
		{
			// TODO: need to unify Type --> Convertion
			Put(messages, typeof(T).FullName);
		}

		/// <summary>Put messages into the queue identified by <c>queueId</c>.</summary>
		public void Put<T>(IEnumerable<T> messages, string queueId)
		{
			throw new NotImplementedException();
		}
	}
}
