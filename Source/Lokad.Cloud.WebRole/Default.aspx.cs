#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using DotNetOpenAuth.OpenId.RelyingParty;

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
			var users = new LokadCloudUserRoles();
			var isAdmin = users.GetAdministrators().Exists(
				user => user.Credential == e.ClaimedIdentifier);

			// if the user isn't listed as an administrator, cancel the login
			if (!isAdmin)
			{
				e.Cancel = true;
			}
		}
	}
}
