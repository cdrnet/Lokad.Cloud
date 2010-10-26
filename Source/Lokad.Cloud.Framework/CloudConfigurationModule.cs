#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Quality;

namespace Lokad.Cloud
{
	/// <summary>
	/// IoC configuration module for Azure storage and management credentials.
	/// Recommended to be loaded either manually or in the appconfig.
	/// </summary>
	/// <remarks>
	/// When only using the storage (O/C mapping) toolkit standalone it is easier
	/// to let the <see cref="Standalone"/> factory create the storage providers on demand.
	/// </remarks>
	/// <seealso cref="CloudModule"/>
	/// <seealso cref="Standalone"/>
	public sealed class CloudConfigurationModule : Module
	{
		/// <summary>Azure storage connection string.</summary>
		[UsedImplicitly]
		public string DataConnectionString { get; set; }

		/// <summary>Azure subscription Id (optional).</summary>
		[UsedImplicitly]
		public string SelfManagementSubscriptionId { get; set; }

		/// <summary>Azure management certificate thumbprint (optional).</summary>
		[UsedImplicitly]
		public string SelfManagementCertificateThumbprint { get; set; }

		public CloudConfigurationModule()
		{
		}

		public CloudConfigurationModule(ICloudConfigurationSettings externalSettings)
		{
			ApplySettings(externalSettings);
		}

		protected override void Load(ContainerBuilder builder)
		{
			if (string.IsNullOrEmpty(DataConnectionString))
			{
				var config = RoleConfigurationSettings.LoadFromRoleEnvironment();
				if (config.HasValue)
				{
					ApplySettings(config.Value);
				}
			}

			// Only register storage components if the storage credentials are OK
			// This will cause exceptions to be thrown quite soon, but this way
			// the roles' OnStart() method returns correctly, allowing the web role
			// to display a warning to the user (the worker is recycled indefinitely
			// as Run() throws almost immediately)

			if (string.IsNullOrEmpty(DataConnectionString))
			{
				return;
			}

			builder.Register(new RoleConfigurationSettings
				{
					DataConnectionString = DataConnectionString,
					SelfManagementSubscriptionId = SelfManagementSubscriptionId,
					SelfManagementCertificateThumbprint = SelfManagementCertificateThumbprint
				}).As<ICloudConfigurationSettings>();
		}

		void ApplySettings(ICloudConfigurationSettings settings)
		{
			DataConnectionString = settings.DataConnectionString;
			SelfManagementSubscriptionId = settings.SelfManagementSubscriptionId;
			SelfManagementCertificateThumbprint = settings.SelfManagementCertificateThumbprint;
		}
	}
}
