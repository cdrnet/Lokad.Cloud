#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Text;
using System.Xml.Linq;
using Lokad.Cloud.Azure.ManagementApiClient;

namespace Lokad.Cloud.Azure
{
	/// <summary>
	/// Azure Management API Provider, Provisioning Provider.
	/// </summary>
	public class AzureManagementProvider : IProvisioningProvider
	{
		readonly ILog _log;

		readonly bool _enabled;
		readonly Maybe<X509Certificate2> _certificate = Maybe<X509Certificate2>.Empty;
		readonly Maybe<string> _deploymentId = Maybe.String;
		readonly Maybe<string> _subscriptionId = Maybe.String;
		
		ManagementStatus _status;
		Maybe<HostedService> _service = Maybe<HostedService>.Empty;
		Maybe<Deployment> _deployment = Maybe<Deployment>.Empty;

		ManagementClient _client;

		//[ThreadStatic]
		IAzureServiceManagement _channel;

		/// <summary>IoC constructor.</summary>>
		public AzureManagementProvider(RoleConfigurationSettings settings, ILog log)
		{
			_log = log;

			// try get settings and certificate
			_deploymentId = CloudEnvironment.AzureDeploymentId;
			_subscriptionId = settings.SelfManagementSubscriptionId ?? Maybe.String;
			var certificateThumbprint = settings.SelfManagementCertificateThumbprint ?? Maybe.String;
			if (certificateThumbprint.HasValue)
			{
				_certificate = CloudEnvironment.GetCertificate(certificateThumbprint.Value);
			}

			// early evaluate management status for intrinsic fault states, to skip further processing
			if (!_deploymentId.HasValue || !_subscriptionId.HasValue || !certificateThumbprint.HasValue)
			{
				_status = ManagementStatus.ConfigurationMissing;
				return;
			}
			if (!_certificate.HasValue)
			{
				_status = ManagementStatus.CertificateMissing;
				return;
			}

			// ok, now try find service matching the deployment
			_enabled = true;
			TryFindDeployment();
		}

		public ManagementStatus Status
		{
			get { return _status; }
		}

		public bool IsAvailable
		{
			get { return _status == ManagementStatus.Available; }
		}

		public Maybe<X509Certificate2> Certificate
		{
			get { return _certificate; }
		}

		public Maybe<string> Subscription
		{
			get { return _subscriptionId; }
		}

		public Maybe<string> DeploymentName
		{
			get { return _deployment.Convert(d => d.Name); }
		}

		public Maybe<string> DeploymentId
		{
			get { return _deployment.Convert(d => d.PrivateID); }
		}

		public Maybe<string> DeploymentLabel
		{
			get { return _deployment.Convert(d => Base64Decode(d.Label)); }
		}

		public Maybe<DeploymentSlot> DeploymentSlot
		{
			get { return _deployment.Convert(d => d.DeploymentSlot); }
		}

		public Maybe<DeploymentStatus> DeploymentStatus
		{
			get { return _deployment.Convert(d => d.Status); }
		}

		public Maybe<string> ServiceName
		{
			get { return _service.Convert(s => s.ServiceName); }
		}

		public Maybe<string> ServiceLabel
		{
			get { return _service.Convert(s => Base64Decode(s.HostedServiceProperties.Label)); }
		}

		public Maybe<int> WorkerInstanceCount
		{
			get { return _deployment.Convert(d => d.RoleInstanceList.Count(ri => ri.RoleName == "Lokad.Cloud.WorkerRole")); }
		}

		public void Update()
		{
			if (!IsAvailable)
			{
				return;
			}

			PrepareRequest();

			_service = _channel.GetHostedServiceWithDetails(_subscriptionId.Value, _service.Value.ServiceName, true);
			_deployment = _service.Value.Deployments.Single(d => d.PrivateID == _deploymentId.Value);
		}

		Maybe<int> IProvisioningProvider.GetWorkerInstanceCount()
		{
			Update();
			return WorkerInstanceCount;
		}

		public void SetWorkerInstanceCount(int count)
		{
			ChangeDeploymentConfiguration(
				config =>
					{
						XAttribute instanceCount;
						try
						{
							// need to be careful about namespaces
							instanceCount = config
								.Descendants()
								.Single(d => d.Name.LocalName == "Role" && d.Attributes().Single(a => a.Name.LocalName == "name").Value == "Lokad.Cloud.WorkerRole")
								.Elements()
								.Single(e => e.Name.LocalName == "Instances")
								.Attributes()
								.Single(a => a.Name.LocalName == "count");
						}
						catch (Exception ex)
						{
							_log.Error(ex, "Azure Self-Management: Unexpected service configuration file format.");
							throw;
						}
						
						var oldCount = instanceCount.Value;
						var newCount = count.ToString();
						_log.InfoFormat("Azure Self-Management: Update worker instance count from {0} to {1}", oldCount, newCount);

						instanceCount.Value = newCount;
					});
		}

		void ChangeDeploymentConfiguration(Action<XElement> updater)
		{
			PrepareRequest();

			_deployment = _channel.GetDeployment(
				_subscriptionId.Value,
				_service.Value.ServiceName,
				_deployment.Value.Name);

			var config = Base64Decode(_deployment.Value.Configuration);
			var xml = XDocument.Parse(config, LoadOptions.SetBaseUri | LoadOptions.PreserveWhitespace);

			updater(xml.Root);

			var newConfig = xml.ToString(SaveOptions.DisableFormatting);

			_channel.ChangeConfiguration(
				_subscriptionId.Value,
				_service.Value.ServiceName,
				_deployment.Value.Name,
				new ChangeConfigurationInput
					{
						Configuration = Base64Encode(newConfig)
					});
		}

		void PrepareRequest()
		{
			if (!_enabled)
			{
				throw new InvalidOperationException("not enabled");
			}

			if (_channel == null)
			{
				if (_client == null)
				{
					_client = new ManagementClient(_certificate.Value);
				}

				_channel = _client.CreateChannel();
			}

			if (_status == ManagementStatus.Unknown)
			{
				TryFindDeployment();
			}

			if (_status != ManagementStatus.Available)
			{
				throw new InvalidOperationException("not operational");
			}
		}

		bool TryFindDeployment()
		{
			if (!_enabled || _status != ManagementStatus.Unknown)
			{
				throw new InvalidOperationException();
			}

			if (_channel == null)
			{
				if (_client == null)
				{
					_client = new ManagementClient(_certificate.Value);
				}

				_channel = _client.CreateChannel();
			}

			var deployments = new List<Pair<Deployment, HostedService>>();
			try
			{
				var hostedServices = _channel.ListHostedServices(_subscriptionId.Value);
				foreach (var hostedService in hostedServices)
				{
					var service = _channel.GetHostedServiceWithDetails(_subscriptionId.Value, hostedService.ServiceName, true);
					if (service == null || service.Deployments == null)
					{
						_log.Warn("Azure Self-Management: skipped unexpected null service or deployment list");
						continue;
					}

					foreach (var deployment in service.Deployments)
					{
						deployments.Add(Tuple.From(deployment, service));
					}
				}

			}
			catch (MessageSecurityException)
			{
				_status = ManagementStatus.AuthenticationFailed;
				return false;
			}
			catch (Exception ex)
			{
				_log.Error(ex, "Azure Self-Management: unexpected error when listing all hosted services.");
				return false;
			}

			if (deployments.Count == 0)
			{
				_log.Warn("Azure Self-Management: found no hosted service deployments");
				_status = ManagementStatus.DeploymentNotFound;
				return false;
			}

			var selfServiceAndDeployment = deployments.FirstOrEmpty(pair => pair.Key.PrivateID == _deploymentId.Value);
			if (!selfServiceAndDeployment.HasValue)
			{
				_log.WarnFormat("Azure Self-Management: no hosted service deployment matches {0}", _deploymentId.Value);
				_status = ManagementStatus.DeploymentNotFound;
				return false;
			}

			_status = ManagementStatus.Available;
			_service = selfServiceAndDeployment.Value.Value;
			_deployment = selfServiceAndDeployment.Value.Key;
			return true;
		}

		static string Base64Decode(string value)
		{
			var bytes = Convert.FromBase64String(value);
			return Encoding.UTF8.GetString(bytes);
		}

		static string Base64Encode(string value)
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			return Convert.ToBase64String(bytes);
		}
	}

	public enum ManagementStatus
	{
		Unknown = 0,
		Available,
		ConfigurationMissing,
		CertificateMissing,
		AuthenticationFailed,
		DeploymentNotFound,
	}
}
