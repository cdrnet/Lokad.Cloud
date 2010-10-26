#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Service Monitoring Instrumentation
	/// </summary>
	public interface IServiceMonitor
	{
		/// <summary>
		/// Monitor starting a server, dispose once its stopped.
		/// </summary>
		IDisposable Monitor(CloudService service);
	}
}
