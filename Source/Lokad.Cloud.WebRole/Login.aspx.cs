#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DotNetOpenId.RelyingParty;

namespace Lokad.Cloud.Web
{
	public partial class Login : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			
		}

		protected void OpenIdLogin_LoggingIn(object sender, OpenIdEventArgs args)
		{
			// TODO: add validation based on 'args.ClaimedIdentifier'
			// (if it can't be done directly in web.config)
		}
	}
}
