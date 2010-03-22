#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	public class LokadCloudUserRoleInfo
	{
		public string Credential { get; set; }
	}

	public class LokadCloudUserRoles
	{
		public IEnumerable<LokadCloudUserRoleInfo> GetAdministrators()
		{
			var admins = CloudEnvironment.IsAvailable
				? RoleEnvironment.GetConfigurationSettingValue("Admins")
				: ConfigurationManager.AppSettings["Admins"];

			foreach (var admin in admins.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries))
			{
				yield return new LokadCloudUserRoleInfo
					{
						Credential = admin
					};
			}
		}
	}
}
