#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Configuration;
using DotNetOpenId.RelyingParty;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	public partial class Login : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			
		}

		protected void OpenIdLogin_OnLoggingIn(object sender, OpenIdEventArgs e)
		{
			var admins = RoleManager.GetConfigurationSetting("Admins");

			foreach(var admin in admins.Split(new [] {" "}, StringSplitOptions.RemoveEmptyEntries))
			{
				if(e.ClaimedIdentifier == admin)
				{
					return;
				}
			}

			// if the user isn't listed as an administrator, cancel the login
			e.Cancel = true;
		}
	}
}
