#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Data.Services.Client;
using System.Net;
using System.Text.RegularExpressions;
using Lokad.Diagnostics;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Azure
{
	/// <summary>
	/// Azure retry policies for corner-situation and server errors.
	/// </summary>
	internal static class AzurePolicies
	{
		/// <summary>
		/// Retry policy to temporarily back off in case of transient Azure server
		/// errors, system overload or in case the denial of service detection system
		/// thinks we're a too heavy user. Blocks the thread while backing off to
		/// prevent further requests for a while (per thread).
		/// </summary>
		public static ActionPolicy TransientServerErrorBackOff { get; private set; }

		/// <summary>Similar to <see cref="TransientServerErrorBackOff"/>, yet
		/// the Table Storage comes with its own set or exceptions/.</summary>
		public static ActionPolicy TransientTableErrorBackOff { get; private set; }

		/// <summary>
		/// Very patient retry policy to deal with container, queue or table instantiation
		/// that happens just after a deletion.
		/// </summary>
		public static ActionPolicy SlowInstantiation { get; private set; }

		// Instrumentation
		static readonly ExecutionCounter _countOnTransientServerError;
		static readonly ExecutionCounter _countOnTransientTableError;
		static readonly ExecutionCounter _countOnSlowInstantiation;

		/// <summary>
		/// Static Constructor
		/// </summary>
		static AzurePolicies()
		{
			// Instrumentation
			ExecutionCounters.Default.RegisterRange(new[]
				{
					_countOnTransientServerError = new ExecutionCounter("Policies.ServerErrorRetryWait", 0, 0),
					_countOnTransientTableError = new ExecutionCounter("Policies.TableErrorRetryWait", 0, 0),
					_countOnSlowInstantiation = new ExecutionCounter("Policies.SlowInstantiationRetryWait", 0, 0)
				});

			// Initialize Policies
			TransientServerErrorBackOff = ActionPolicy.With(TransientServerErrorExceptionFilter)
				.Retry(30, OnTransientServerErrorRetry);

			TransientTableErrorBackOff = ActionPolicy.With(TransientTableErrorExceptionFilter)
				.Retry(30, OnTransientTableErrorRetry);

			SlowInstantiation = ActionPolicy.With(SlowInstantiationExceptionFilter)
				.Retry(30, OnSlowInstantiationRetry);
		}

		static void OnTransientServerErrorRetry(Exception exception, int count)
		{
			// NOTE: we can't log here, since logging would fail as well

			var timestamp = _countOnTransientServerError.Open();

			// quadratic backoff, capped at 5 minutes
			var c = count + 1;
			SystemUtil.Sleep(Math.Min(300, c * c).Seconds());

			_countOnTransientServerError.Close(timestamp);
		}

		static void OnTransientTableErrorRetry(Exception exception, int count)
		{
			// NOTE: we can't log here, since logging would fail as well

			var timestamp = _countOnTransientTableError.Open();

			// quadratic backoff, capped at 5 minutes
			var c = count + 1;
			SystemUtil.Sleep(Math.Min(300, c * c).Seconds());

			_countOnTransientTableError.Close(timestamp);
		}

		static void OnSlowInstantiationRetry(Exception exception, int count)
		{
			var timestamp = _countOnSlowInstantiation.Open();

			// linear backoff
			SystemUtil.Sleep((100 * count).Milliseconds());

			_countOnSlowInstantiation.Close(timestamp);
		}

		static bool TransientServerErrorExceptionFilter(Exception exception)
		{
			var serverException = exception as StorageServerException;
			if (serverException == null)
			{
				// we only handle server exceptions
				return false;
			}

			var errorCode = serverException.ErrorCode;
			var errorString = serverException.ExtendedErrorInformation.ErrorCode;

			if (errorCode == StorageErrorCode.ServiceInternalError
				|| errorCode == StorageErrorCode.ServiceTimeout
				|| errorString == StorageErrorCodeStrings.InternalError
				|| errorString == StorageErrorCodeStrings.ServerBusy
				|| errorString == StorageErrorCodeStrings.OperationTimedOut)
			{
				return true;
			}

			return false;
		}

		static bool TransientTableErrorExceptionFilter(Exception exception)
		{
			// HACK: StorageClient does not catch very well, internal errors of the table storage.
			// Hence we end up here manually catching exception that should have been correctly 
			// typed by the StorageClient, such as:
			// The remote server returned an error: (500) Internal Server Error.
			var webException = exception as WebException;
			if (null != webException && webException.Status == WebExceptionStatus.ProtocolError)
			{
				return true;
			}

			var serverException = exception as DataServiceRequestException;
			if (serverException == null)
			{
				// we only handle server exceptions
				return false;
			}

			var errorCode = GetErrorCode(serverException);

			if (errorCode == StorageErrorCodeStrings.InternalError
				|| errorCode == StorageErrorCodeStrings.ServerBusy
				|| errorCode == TableErrorCodeStrings.TableServerOutOfMemory)
				// OperationTimedOut is ignored on purpose (to be more precisely handled in the provider)
				// Indeed, time-out is aggressively set at 30s for the Table Storage
				// hence, if a request fails, we should rather reduce the number for transferred entities
			{
				return true;
			}

			return false;
		}

		static bool SlowInstantiationExceptionFilter(Exception exception)
		{
			var storageException = exception as StorageClientException;

			// Blob Storage or Queue Storage exceptions
			// Table Storage may throw exception of type 'StorageClientException'
			if (null != storageException)
			{
				var errorCode = storageException.ErrorCode;
				var errorString = storageException.ExtendedErrorInformation.ErrorCode;

				// those 'client' exceptions reflects server-side problem (delayed instantiation)
				if (errorCode == StorageErrorCode.ResourceNotFound
					|| errorCode == StorageErrorCode.ContainerNotFound
						|| errorString == QueueErrorCodeStrings.QueueNotFound
							|| errorString == StorageErrorCodeStrings.InternalError
								|| errorString == StorageErrorCodeStrings.ServerBusy
									|| errorString == TableErrorCodeStrings.TableServerOutOfMemory
										|| errorString == TableErrorCodeStrings.TableBeingDeleted)
				{
					return true;
				}
			}

			var tableException = exception as DataServiceQueryException;

			// Table Storage may also throw exception of type 'DataServiceQueryException'.
			if (null != tableException)
			{
				var errorString = GetErrorCode(tableException);

				if(errorString == TableErrorCodeStrings.TableBeingDeleted ||
					errorString == TableErrorCodeStrings.TableServerOutOfMemory)
				{
					return true;
				}
			}

			return false;
		}

		public static string GetErrorCode(DataServiceRequestException ex)
		{
			var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);
			var match = r.Match(ex.InnerException.Message);
			return match.Groups[1].Value;
		}

		// HACK: just dupplicating the other overload of 'GetErrorCode'
		public static string GetErrorCode(DataServiceQueryException ex)
		{
			var r = new Regex(@"<code>(\w+)</code>", RegexOptions.IgnoreCase);
			var match = r.Match(ex.InnerException.Message);
			return match.Groups[1].Value;
		}
	}
}
