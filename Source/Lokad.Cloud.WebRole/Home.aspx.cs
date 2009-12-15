#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Web
{
	public partial class Home: System.Web.UI.Page
	{
		readonly LokadCloudVersion _version = GlobalSetup.Container.Resolve<LokadCloudVersion>();
		readonly LokadCloudUserRoles _users = GlobalSetup.Container.Resolve<LokadCloudUserRoles>(); 

		protected void Page_Load(object sender, EventArgs e)
		{
			AdminsView.DataSource = _users.GetAdministrators();
			AdminsView.DataBind();

			StorageAccountNameLabel.Text = GlobalSetup.StorageAccountName;
			LokadCloudVersionLabel.Text = _version.RunningVersion.ToString();

			switch (_version.VersionState)
			{
				case LokadCloudVersionState.UpToDate:
					LokadCloudUpdateStatusLabel.Text = "Up-to-date";
					DownloadLokadCloudLink.Visible = false;
					break;

				case LokadCloudVersionState.UpdateAvailable:
					LokadCloudUpdateStatusLabel.Text = String.Format(
						"New version available ({0})",
						_version.NewestVersion.Value);
					DownloadLokadCloudLink.Visible = true;
					DownloadLokadCloudLink.NavigateUrl = _version.DownloadUri.ToString();
					break;

				default:
					LokadCloudUpdateStatusLabel.Text = "Could not retrieve version info";
					DownloadLokadCloudLink.Visible = false;
					break;
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
