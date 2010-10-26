#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

namespace Lokad.Cloud
{
	/// <summary>Collects objects that absolutely need to be disposed
	/// before the runtime gets shut down.</summary>
	/// <remarks>
	/// There is no garanty that registered objects will actually be disposed.
	/// When a VM is shutdown, a small grace period (30s) is offered to clean-up
	/// resources before the OS itself is aborted. The runtime finalizer
	/// should be kept for very critical clean-ups to be performed during the
	/// grace period.
	/// 
	/// Typically, the runtime finalizer is used to abandon in-process queue
	/// messages and lease on blobs. Any extra object that you register here
	/// is likely to negatively impact more prioritary clean-ups. Use with care.
	/// 
	/// Implementations must be thread-safe.
	/// </remarks>
	public interface IRuntimeFinalizer
	{
		/// <summary>Register a object for high priority finalization if runtime is terminating.</summary>
		/// <remarks>The method is idempotent, once an object is registered,
		/// registering the object again has no effect.</remarks>
		void Register(IDisposable obj);

		/// <summary>Unregister a object from high priority finalization.</summary>
		/// <remarks>The method is idempotent, once an object is unregistered,
		/// unregistering the object again has no effect.</remarks>
		void Unregister(IDisposable obj);

		/// <summary>
		/// Finalize high-priority resources hold by the runtime. This method
		/// should only be called ONCE upon runtime finalization.
		/// </summary>
		void FinalizeRuntime();
	}
}