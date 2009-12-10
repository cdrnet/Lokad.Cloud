#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Web.UI;
using Lokad.Cloud.Management;

namespace Lokad.Cloud.Web
{
	public partial class Config : Page
	{
		readonly CloudConfiguration _cloudConfig = GlobalSetup.Container.Resolve<CloudConfiguration>();

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				ConfigurationBox.Text = _cloudConfig.GetConfigurationString();
			}
		}

		protected void SaveConfigButton_OnClick(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(ConfigurationBox.Text))
			{
				_cloudConfig.SetConfiguration(ConfigurationBox.Text);
			}
			else
			{
				// don't save empty empty configuration, delete it (so that it will be ignored)
				_cloudConfig.RemoveConfiguration();
			}

			SavedLabel.Visible = true;
		}
	}
}
