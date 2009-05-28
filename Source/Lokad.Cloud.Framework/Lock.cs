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
	/// <summary>Cloud lock. The scope of the lock is application-wide.</summary>
	/// <remarks>
	/// Typical usage pattern is
	/// <code>
	/// using(new Lock("mylock"))
	/// {
	///		// safe execution segment
	/// }
	/// </code>
	/// </remarks>
	public class Lock : IDisposable
	{
		/// <summary>Try to acquire the lock. Call is blocked until the lock is
		/// acquired.</summary>
		/// <param name="lockId">Unique lock identifier.</param>
		public Lock(string lockId)
		{
			
		}

		/// <summary>Try to acquire the lock with the specified timespan. If the lock
		/// could not be acquired a <see cref="TimeoutException"/> is thrown.</summary>
		/// <param name="lockId"></param>
		/// <param name="timeout"></param>
		public Lock(string lockId, TimeSpan timeout)
		{

		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
