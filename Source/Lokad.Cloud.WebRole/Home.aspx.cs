#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using Lokad.Cloud.Provisioning.Azure;
using Lokad.Cloud.Provisioning.Azure.Entities;

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

			LokadCloudVersionLabel.Text = _version.RunningVersion.ToString();
			StorageAccountLabel.Text = GlobalSetup.StorageAccountName;

			// Management
			var management = GlobalSetup.Container.Resolve<ProvisioningProvider>();
			management.Update();

			SubscriptionLabel.Text = management.Subscription.GetValue("unknown");
			CertificatesLabel.Text = management.Certificate.Convert(c => c.SubjectName.Name, "unknown");
			CertificateThumbprintLabel.Text = management.Certificate.Convert(c => c.Thumbprint, "unknown");

			// Management Warnings
			_managementWarningPanel.Visible = true;
			switch(management.Status)
			{
				case ManagementStatus.ConfigurationMissing:
					_managementConfigurationMissing.Visible = true;
					break;
				case ManagementStatus.CertificateMissing:
					_managementCertificateMissing.Visible = true;
					break;
				case ManagementStatus.AuthenticationFailed:
					_managementAuthenticationFailed.Visible = true;
					break;
				case ManagementStatus.DeploymentNotFound:
				case ManagementStatus.Unknown:
					_managementDeploymentNotFound.Visible = true;
					break;
				default:
					_managementWarningPanel.Visible = false;
					
					break;
			}

			// Worker Instance Update
			WorkerInstancesPanel.Visible = false;
			DeploymentStatusPanel.Visible = false;
			if (management.Status == ManagementStatus.Available)
			{
				if(management.DeploymentStatus == DeploymentStatus.Running)
				{
					WorkerInstancesPanel.Visible = true;
				}
				else
				{
					DeploymentStatusPanel.Visible = true;
				}
			}

			// Deployment Label
			DeploymentLabel.Text = CloudEnvironment.AzureDeploymentId.GetValue("unknown");
			var deploymentLabel = management.DeploymentLabel;
			if (deploymentLabel.HasValue)
			{
				DeploymentLabel.Text = String.Format("{0} ({1})", deploymentLabel.Value, DeploymentLabel.Text);
			}
			var deploymentSlot = management.DeploymentSlot;
			if (deploymentSlot.HasValue)
			{
				DeploymentLabel.Text = String.Format("{0}: {1}", deploymentSlot.Value, DeploymentLabel.Text);
			}
			else if (CloudEnvironment.AzureDeploymentId.Convert(id => id.StartsWith("deployment("), false))
			{
				DeploymentLabel.Text = String.Format("Development Fabric: {0}", DeploymentLabel.Text);
			}

			// Service Label
			HostedServiceLabel.Text = management.ServiceName.GetValue("unknown");
			var serviceLabel = management.ServiceLabel;
			if (serviceLabel.HasValue)
			{
				HostedServiceLabel.Text = String.Format("{0} ({1})", serviceLabel.Value, HostedServiceLabel.Text);
			}

			// Worker Instance Count
			var instanceCount = management.WorkerInstanceCount;
			if (!instanceCount.HasValue)
			{
				// RoleEnvironment always returns 0 if it fails to evaluate it,
				// which is if the role defines no (internal) endpoints.
				// So we fall back to Empty and thus "unknown" in this case.
				instanceCount = CloudEnvironment.AzureWorkerInstanceCount
					.Combine(c => c > 0 ? c : Maybe<int>.Empty);
			}
			AzureWorkerInstancesLabel.Text = instanceCount.Convert(c => c.ToString(), "unknown");
			if (!Page.IsPostBack)
			{
				WorkerInstancesBox.Text = instanceCount.Convert(c => c.ToString(), "0");
			}

			// Lokad.Cloud Version
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

		protected void SetWorkerInstancesButton_Click(object sender, EventArgs e)
		{
			Page.Validate("set");
			if (!Page.IsValid)
			{
				return;
			}

			var management = GlobalSetup.Container.Resolve<ProvisioningProvider>();
			if (!management.IsAvailable)
			{
				return;
			}

			management.SetWorkerInstanceCount(int.Parse(WorkerInstancesBox.Text));

			WorkerInstancesPanel.Visible = false;
			DeploymentStatusPanel.Visible = true;
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
