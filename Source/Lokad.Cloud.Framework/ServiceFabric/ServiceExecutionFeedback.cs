#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.ServiceFabric
{
	/// <summary>
	/// The execution result of a scheduled action, providing information that
	/// might be considered for further scheduling.
	/// </summary>
	public enum ServiceExecutionFeedback
	{
		/// <summary>
		/// No information available or the service is not interested in providing
		/// any details.
		/// </summary>
		DontCare = 0,

		/// <summary>
		/// The service knows or assumes that there is more work available.
		/// </summary>
		WorkAvailable,

		/// <summary>
		/// The service did some work, but knows or assumes that there is no more work
		/// available.
		/// </summary>
		DoneForNow,

		/// <summary>
		/// The service skipped without doing any work (and expects the same for
		/// successive calls).
		/// </summary>
		Skipped,

		/// <summary>
		/// The service failed with a fatal error.
		/// </summary>
		Failed
	}
}