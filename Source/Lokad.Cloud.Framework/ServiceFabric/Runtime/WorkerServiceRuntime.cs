#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>
	/// Worker Runtime for Cloud Services
	/// </summary>
	public class WorkerServiceRuntime
	{
		IsolatedWorker _worker;

		/// <summary>
		/// Start up the runtime. This step is required before calling Run.
		/// </summary>
		public void StartRuntime()
		{
			RoleEnvironment.Changing += OnRoleEnvironmentChanging;
		}

		/// <summary>Shutdown the runtime.</summary>
		public void ShutdownRuntime()
		{
			RoleEnvironment.Changing -= OnRoleEnvironmentChanging;

			if(null != _worker)
			{
				_worker.OnStop();
			}
		}

		/// <summary>Runtime Main Thread.</summary>
		public void Run()
		{
			var restartPolicy = new NoRestartFloodPolicy(isHealthy => { });
			restartPolicy.Do(() =>
			{
				_worker = new IsolatedWorker();
				return _worker.DoWork();
			});
		}

		void OnRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
		{
			// we restart all workers if the configuration changed (e.g. the storage account)
			// for now.

			// We do not request a recycle if only the topology changed, 
			// e.g. if some instances have been removed or added.
			var configChanges = e.Changes.OfType<RoleEnvironmentConfigurationSettingChange>();

			if(configChanges.Any())
			{
				RoleEnvironment.RequestRecycle();
			}
		}
	}
}
