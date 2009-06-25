#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	public partial class _Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			AdminsView.DataSource = GetAdmins();
			AdminsView.DataBind();
		}

		IEnumerable<object> GetAdmins()
		{
			var admins = RoleManager.GetConfigurationSetting("Admins");

			foreach(var admin in admins.Split(new [] {" "}, StringSplitOptions.RemoveEmptyEntries))
			{
				yield return new
					{
						Credential = admin
					};
			}
		}
	}
}
