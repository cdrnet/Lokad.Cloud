#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>
	/// Entry point, hosting the service fabric with one or more
	/// continuously running isolated runtimes.
	/// </summary>
	public class ServiceFabricHost
	{
		readonly NoRestartFloodPolicy _restartPolicy;
		volatile IsolatedSingleRuntimeHost _primaryRuntimeHost;

		public ServiceFabricHost()
		{
			_restartPolicy = new NoRestartFloodPolicy();
		}

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

			_restartPolicy.IsStopRequested = true;

			if(null != _primaryRuntimeHost)
			{
				_primaryRuntimeHost.Stop();
			}
		}

		/// <summary>Runtime Main Thread.</summary>
		public void Run()
		{
			// restart policy cease restarts if stop is requested
			_restartPolicy.Do(() =>
				{
					_primaryRuntimeHost = new IsolatedSingleRuntimeHost();
					return _primaryRuntimeHost.Run();
				});
		}

		static void OnRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
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
