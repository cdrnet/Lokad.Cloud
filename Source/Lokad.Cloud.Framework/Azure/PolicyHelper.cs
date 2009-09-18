#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Azure
{
	/// <summary>Custom retry policy for corner-situation.</summary>
	public static class PolicyHelper
	{
		static ActionPolicy _slowInstantiation;

		/// <summary>Very patient retry policy to deal with container or queue
		/// instantiation that happens just after a deletion.</summary>
		public static ActionPolicy SlowInstantiation
		{
			get
			{
				// design is immutable, no concurrency issue here.
				if (null == _slowInstantiation)
				{
					_slowInstantiation =
						ActionPolicy.With(HandleSlowInstantiationException)
									.Retry(30, (e, i) => SystemUtil.Sleep((100 * i).Milliseconds()));
				}

				return _slowInstantiation;
			}
		}

		static bool HandleSlowInstantiationException(Exception ex)
		{
			if (ex is StorageServerException)
				return true;

			// those 'client' exceptions reflects server-side problem (delayed instantiation)
			if (ex is StorageClientException)
			{
				var exc = ex as StorageClientException;

				if (exc.ExtendedErrorInformation.ErrorCode == QueueErrorCodeStrings.QueueNotFound ||
					exc.ErrorCode == StorageErrorCode.ContainerNotFound)
				{
					return true;
				}
			}

			return false;
		}
	}
}
