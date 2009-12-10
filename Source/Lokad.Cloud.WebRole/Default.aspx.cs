#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Configuration;
using DotNetOpenAuth.OpenId.RelyingParty;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Lokad.Cloud.Web
{
	public partial class Login : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			// Verify storage credentials
			if(!Page.IsPostBack)
			{
				var verifier = new StorageCredentialsVerifier(GlobalSetup.Container);
				_credentialsWarningPanel.Visible = !verifier.VerifyCredentials();
			}

			_openIdLogin.Focus();
		}

		protected void OpenIdLogin_OnLoggingIn(object sender, OpenIdEventArgs e)
		{
			// HACK: logic to retrieve admins is duplicated with 'Default.aspx'
			var admins = string.Empty;
			if (RoleEnvironment.IsAvailable)
			{
				admins = RoleEnvironment.GetConfigurationSettingValue("Admins");
			}
			else
			{
				admins = ConfigurationManager.AppSettings["Admins"];
			}
			
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
