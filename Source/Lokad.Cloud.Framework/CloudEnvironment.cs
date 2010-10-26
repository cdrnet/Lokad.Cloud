#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud
{
	/// <summary>
	/// Cloud Environment Helper
	/// </summary>
	/// <remarks>
	/// Providing functionality of Azure <see cref="RoleEnvironment"/>,
	/// but more neutral and resilient to missing runtime.
	/// </remarks>
	public static class CloudEnvironment
	{
		readonly static bool _runtimeAvailable;

		static CloudEnvironment()
		{
			try
			{
				_runtimeAvailable = RoleEnvironment.IsAvailable;
			}
			catch (TypeInitializationException)
			{
				_runtimeAvailable = false;
			}
		}

		/// <summary>
		/// Indicates whether the instance is running in the Cloud environment.
		/// </summary>
		public static bool IsAvailable
		{
			get { return _runtimeAvailable; }
		}

		/// <summary>
		/// Cloud Worker Key
		/// </summary>
		public static string PartitionKey
		{
			get { return System.Net.Dns.GetHostName(); }
		}

		/// <summary>
		/// ID of the Cloud Worker Instances
		/// </summary>
		public static Maybe<string> AzureCurrentInstanceId
		{
			get { return _runtimeAvailable ? RoleEnvironment.CurrentRoleInstance.Id : Maybe.String; }
		}

		public static Maybe<string> AzureDeploymentId
		{
			get { return _runtimeAvailable ? RoleEnvironment.DeploymentId : Maybe.String; }
		}

		public static Maybe<int> AzureWorkerInstanceCount
		{
			get
			{
				if(!_runtimeAvailable)
				{
					return Maybe<int>.Empty;
				}

				Role workerRole;
				if(!RoleEnvironment.Roles.TryGetValue("Lokad.Cloud.WorkerRole", out workerRole))
				{
					return Maybe<int>.Empty;
				}

				return workerRole.Instances.Count;
			}
		}

		/// <summary>
		/// Retrieves the root path of a named local resource.
		/// </summary>
		public static string GetLocalStoragePath(string resourceName)
		{
			if (IsAvailable)
			{
				return RoleEnvironment.GetLocalResource(resourceName).RootPath;
			}

			var dir = Path.Combine(Path.GetTempPath(), resourceName);
			Directory.CreateDirectory(dir);
			return dir;
		}

		public static Maybe<X509Certificate2> GetCertificate(string thumbprint)
		{
			var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			try
			{
				store.Open(OpenFlags.ReadOnly);
				var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
				if(certs.Count != 1)
				{
					return Maybe<X509Certificate2>.Empty;
				}

				return certs[0];
			}
			finally
			{
				store.Close();
			}
		}

		///<summary>
		/// Retreives the configuration setting from the <see cref="RoleEnvironment"/>.
		///</summary>
		///<param name="configurationSettingName">Name of the configuration setting</param>
		///<returns>configuration value, or an empty result, if the environment is not present, or the value is null or empty</returns>
		public static Maybe<string> GetConfigurationSetting(string configurationSettingName)
		{
			if (!_runtimeAvailable)
			{
				return Maybe<string>.Empty;
			}

			try
			{
				var value = RoleEnvironment.GetConfigurationSettingValue(configurationSettingName);
				if (!String.IsNullOrEmpty(value))
				{
					value = value.Trim();
				}
				if (String.IsNullOrEmpty(value))
				{
					value = null;
				}
				return value;
			}
			catch (RoleEnvironmentException)
			{
				return Maybe<string>.Empty;
				// setting was removed from the csdef, skip
				// (logging is usually not available at that stage)
			}
		}

		public static bool HasSecureEndpoint()
		{
			if (!_runtimeAvailable)
			{
				return false;
			}

			return RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.ContainsKey("HttpsIn");
		}
	}
}