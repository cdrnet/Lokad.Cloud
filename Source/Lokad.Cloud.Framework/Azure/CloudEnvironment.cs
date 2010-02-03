#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Azure
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
	}
}
