#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.ServiceFabric.Runtime;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
	/// <summary>Entry point of Lokad.Cloud.</summary>
	public class WorkerRole : RoleEntryPoint
	{
		readonly ServiceFabricHost _serviceFabricHost;

		public WorkerRole()
		{
			_serviceFabricHost = new ServiceFabricHost();
		}

		/// <summary>
		/// Called by Windows Azure to initialize the role instance.
		/// </summary>
		/// <returns>
		/// True if initialization succeeds, False if it fails. The default implementation returns True.
		/// </returns>
		/// <remarks>
		/// <para>Any exception that occurs within the OnStart method is an unhandled exception.</para>
		/// </remarks>
		public override bool OnStart()
		{
			_serviceFabricHost.StartRuntime();
			return true;
		}

		/// <summary>
		/// Called by Windows Azure when the role instance is to be stopped. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Override the OnStop method to implement any code your role requires to
		/// shut down in an orderly fashion.
		/// </para>
		/// <para>
		/// This method must return within certain period of time. If it does not,
		/// Windows Azure will stop the role instance.
		/// </para>
		/// <para>
		/// A web role can include shutdown sequence code in the ASP.NET
		/// Application_End method instead of the OnStop method. Application_End is
		/// called before the Stopping event is raised or the OnStop method is called.
		/// </para>
		/// <para>
		/// Any exception that occurs within the OnStop method is an unhandled
		/// exception.
		/// </para>
		/// </remarks>
		public override void OnStop()
		{
			_serviceFabricHost.ShutdownRuntime();
		}

		/// <summary>
		/// Called by Windows Azure after the role instance has been initialized. This
		/// method serves as the main thread of execution for your role.
		/// </summary>
		/// <remarks>
		/// <para>The role recycles when the Run method returns.</para>
		/// <para>Any exception that occurs within the Run method is an unhandled exception.</para>
		/// </remarks>
		public override void Run()
		{
			_serviceFabricHost.Run();
		}
	}
}
