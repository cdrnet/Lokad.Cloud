#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Lokad.Cloud.Management.Azure
{
	/// <summary>
	/// Azure retry policies for corner-situation and server errors.
	/// </summary>
	public static class AzureManagementPolicies
	{
		/// <summary>
		/// Retry policy to temporarily back off in case of transient Azure server
		/// errors, system overload or in case the denial of service detection system
		/// thinks we're a too heavy user. Blocks the thread while backing off to
		/// prevent further requests for a while (per thread).
		/// </summary>
		public static ActionPolicy TransientServerErrorBackOff { get; private set; }

		/// <summary>
		/// Static Constructor
		/// </summary>
		static AzureManagementPolicies()
		{
			// Initialize Policies
			TransientServerErrorBackOff = ActionPolicy.With(TransientServerErrorExceptionFilter)
				.Retry(30, OnTransientServerErrorRetry);
		}

		static void OnTransientServerErrorRetry(Exception exception, int count)
		{
			// quadratic backoff, capped at 5 minutes
			var c = count + 1;
			SystemUtil.Sleep(Math.Min(300, c * c).Seconds());
		}

		static bool TransientServerErrorExceptionFilter(Exception exception)
		{
			// NOTE: We observed Azure hiccups that caused transport security
			// to momentarily fail, causing a MessageSecurity exception (#1405).
			// We thus tread this exception as a transient error.
			// In case it is a permanent error it will still show up,
			// although delayed by the 30 retrials.

			if (exception is EndpointNotFoundException
				|| exception is TimeoutException
				|| exception is MessageSecurityException)
			{
				return true;
			}

			return false;
		}
	}
}