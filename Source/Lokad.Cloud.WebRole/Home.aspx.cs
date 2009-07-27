#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Configuration;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	public partial class Home: System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			AdminsView.DataSource = GetAdmins();
			AdminsView.DataBind();
			LokadCloudVersion.DataBind();
		}

		IEnumerable<object> GetAdmins()
		{
			// HACK: logic to retrieve admins is duplicated with 'Default.aspx'
			var admins = string.Empty;
			if (RoleManager.IsRoleManagerRunning)
			{
				admins = RoleManager.GetConfigurationSetting("Admins");
			}
			else
			{
				admins = ConfigurationManager.AppSettings["Admins"];
			}

			foreach(var admin in admins.Split(new [] {" "}, StringSplitOptions.RemoveEmptyEntries))
			{
				yield return new
					{
						Credential = admin
					};
			}
		}

		protected void ClearCache_Click(object sender, EventArgs e)
		{
			var keys = new List<string>();

			// retrieve application Cache enumerator
			var enumerator = Cache.GetEnumerator();

			// copy all keys that currently exist in Cache
			while (enumerator.MoveNext())
			{
				keys.Add(enumerator.Key.ToString());
			}

			// delete every key from cache
			for (int i = 0; i < keys.Count; i++)
			{
				Cache.Remove(keys[i]);
			}
		}
	}
}
