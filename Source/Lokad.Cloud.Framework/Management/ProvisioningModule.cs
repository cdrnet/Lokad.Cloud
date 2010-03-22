#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;

namespace Lokad.Cloud.Management
{
	/// <summary>IoC module for loading the Management API providers.</summary>
	public class ProvisioningModule : Module
	{
		readonly Maybe<RoleConfigurationSettings> _settings;

		/// <summary>Regular IoC constructor.</summary>
		public ProvisioningModule(Maybe<RoleConfigurationSettings> externalSettings)
		{
			_settings = externalSettings;
			if (!_settings.HasValue)
			{
				_settings = RoleConfigurationSettings.LoadFromRoleEnvironment();
			}
		}

		protected override void Load(ContainerBuilder moduleBuilder)
		{
			if(!_settings.HasValue)
			{
				return;
			}
			var settings = _settings.Value;

			moduleBuilder.Register(c => new CloudProvisioning(settings, c.Resolve<ILog>()))
				.As<CloudProvisioning, IProvisioningProvider>()
				.SingletonScoped();
		}
	}
}