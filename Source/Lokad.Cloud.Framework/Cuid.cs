#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

namespace Lokad.Cloud.Framework
{
	/// <summary>Compact unique identier class that provides unique values (incremented) in 
	/// a scalable manner. Compared to <see cref="Guid"/>, this class is intended as a way 
	/// to provide much more compact identifier.</summary>
	/// <remarks>
	/// Scalability is achieved through an exponential identifier allocation pattern.
	/// </remarks>
	public static class Cuid
	{
		/// <summary>Returns a unique identifier.</summary>
		/// <param name="counterId">Name of the counter being incremented.</param>
		/// <returns>This value is unique and won't be returned any other Azure
		/// role that request the value.</returns>
		/// <remarks>
		/// If the counter does not exist, it gets created by the first call.
		/// Counter returns integers that a strictly increasing. If
		/// there are concurrent call the counter may skip values between calls
		/// (do not expect a small garanted <c>+1</c> increment each time the
		/// counter is increased).
		/// </remarks>
		public static long Next(string counterId)
		{
			throw new NotImplementedException();
		}
	}
}
