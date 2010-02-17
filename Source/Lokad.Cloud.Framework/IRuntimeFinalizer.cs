#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;

namespace Lokad.Cloud
{
	// #128 drafting a provider that will handle speedy-runtime finalization

	/// <summary>Collects objects that absolutely need to be disposed
	/// before the runtime gets shut down.</summary>
	public interface IRuntimeFinalizer
	{
		/// <summary>Register a object for high priority finalization if runtime is terminating.</summary>
		void Register(IDisposable obj);

		/// <summary>Unregister a object from high priority finalization.</summary>
		void Unregister(IDisposable obj);

		// method will be provided the implementation, not supposed to be accessible
		// from 'collectible' objects.
		// void FinalizeRuntime();
	}
}
