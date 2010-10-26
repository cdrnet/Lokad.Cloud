#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Storage.Providers
{
	internal static class TaskFactoryExtensions
	{
		internal static Task<TResult> FromAsyncRetryWithResult<TResult>(this TaskFactory factory, Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, RetryPolicy retryPolicy, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions = TaskCreationOptions.None)
		{
			var completionSource = new TaskCompletionSource<TResult>(state, creationOptions);
			RetryInternalWithResult(factory, f => f.FromAsync(beginMethod, endMethod, null), completionSource, cancellationToken, retryPolicy(), 0);
			return completionSource.Task;
		}

		internal static Task FromAsyncRetry(this TaskFactory factory, Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, RetryPolicy retryPolicy, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions = TaskCreationOptions.None)
		{
			var completionSource = new TaskCompletionSource<object>(state, creationOptions);
			RetryInternal(factory, f => f.FromAsync(beginMethod, endMethod, null), completionSource, cancellationToken, retryPolicy(), 0);
			return completionSource.Task;
		}

		private static void RetryInternalWithResult<TResult>(TaskFactory factory, Func<TaskFactory, Task<TResult>> taskProvider, TaskCompletionSource<TResult> completionSource, CancellationToken cancellationToken, ShouldRetry shouldRetry, int trial)
		{
			var task = taskProvider(factory);
			task.ContinueWith(t => completionSource.TrySetResult(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
			task.ContinueWith(t => completionSource.TrySetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
			task.ContinueWith(t =>
				{
					TimeSpan delay;
					if (!shouldRetry(trial, t.Exception, out delay))
					{
						completionSource.TrySetException(t.Exception.InnerExceptions);
					}
					else
					{
						StartAsyncDelay(delay, null, cancellationToken)
							.ContinueWith(delayTask =>
								{
									if (delayTask.IsCanceled)
									{
										completionSource.TrySetCanceled();
									}
									else
									{
										RetryInternalWithResult(factory, taskProvider, completionSource, cancellationToken, shouldRetry, trial + 1);
									}
								});
					}
				}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private static void RetryInternal(TaskFactory factory, Func<TaskFactory, Task> taskProvider, TaskCompletionSource<object> completionSource, CancellationToken cancellationToken, ShouldRetry shouldRetry, int trial)
		{
			var task = taskProvider(factory);
			task.ContinueWith(t => completionSource.TrySetResult(null), TaskContinuationOptions.OnlyOnRanToCompletion);
			task.ContinueWith(t => completionSource.TrySetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
			task.ContinueWith(t =>
				{
					TimeSpan delay;
					if (!shouldRetry(trial, t.Exception, out delay))
					{
						completionSource.TrySetException(t.Exception.InnerExceptions);
					}
					else
					{
						StartAsyncDelay(delay, null, cancellationToken)
							.ContinueWith(delayTask =>
								{
									if (delayTask.IsCanceled)
									{
										completionSource.TrySetCanceled();
									}
									else
									{
										RetryInternal(factory, taskProvider, completionSource, cancellationToken, shouldRetry, trial + 1);
									}
								});
					}
				}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private static Task StartAsyncDelay(TimeSpan delay, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions = TaskCreationOptions.None)
		{
			var completionSource = new TaskCompletionSource<object>(state, creationOptions);
			var cancellationTokenRegistration = default(CancellationTokenRegistration);

			var timer = new Timer(self =>
			{
				cancellationTokenRegistration.Dispose();
				((Timer)self).Dispose();
				completionSource.TrySetResult(null);
			});

			if (cancellationToken.CanBeCanceled)
			{
				cancellationTokenRegistration = cancellationToken.Register(() =>
				{
					timer.Dispose();
					completionSource.TrySetCanceled();
				});
			}

			timer.Change(delay, TimeSpan.FromMilliseconds(-1));
			return completionSource.Task;
		}
	}
}
