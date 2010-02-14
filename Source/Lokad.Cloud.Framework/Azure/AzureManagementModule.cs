using Autofac.Builder;

namespace Lokad.Cloud.Azure
{
	public class AzureManagementModule : Module
	{
		readonly Maybe<RoleConfigurationSettings> _settings;

		public AzureManagementModule()
			: this(Maybe<RoleConfigurationSettings>.Empty)
		{
		}

		public AzureManagementModule(Maybe<RoleConfigurationSettings> externalSettings)
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

			moduleBuilder.Register(c => new AzureManagementProvider(settings, c.Resolve<ILog>()))
				.As<AzureManagementProvider, IProvisioningProvider>()
				.SingletonScoped();
		}
	}
}
