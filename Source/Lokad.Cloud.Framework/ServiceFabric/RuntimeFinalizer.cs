#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;

namespace Lokad.Cloud.ServiceFabric
{
	/// <summary>High-priority runtime finalizer. Attempts to finalize key cloud resources
	/// when the runtime is forcibly shut down.</summary>
	public class RuntimeFinalizer : IRuntimeFinalizer
	{
		/// <summary>Locking object used to ensure the thread safety of instance.</summary>
		readonly object _sync;

		/// <summary>Collections of objects to be disposed on runtime finalization.</summary>
		readonly HashSet<IDisposable> _disposables;

		bool _isRuntimeFinalized;

		public void Register(IDisposable obj)
		{
			lock(_sync)
			{
				if(_isRuntimeFinalized)
				{
					throw new InvalidOperationException("Runtime already finalized.");	
				}

				_disposables.Add(obj);
			}
		}

		public void Unregister(IDisposable obj)
		{
			lock (_sync)
			{
				if (_isRuntimeFinalized)
				{
					throw new InvalidOperationException("Runtime already finalized.");
				}

				_disposables.Remove(obj);
			}
		}

		public void FinalizeRuntime()
		{
			lock (_sync)
			{
				if (!_isRuntimeFinalized)
				{
					_isRuntimeFinalized = true;

					foreach (var disposable in _disposables)
					{
						disposable.Dispose();
					}
				}

				// ignore multiple calls to finalization
			}
		}

		/// <summary>IoC constructor.</summary>
		public RuntimeFinalizer()
		{
			_sync = new object();
			_disposables = new HashSet<IDisposable>();
		}
	}
}
