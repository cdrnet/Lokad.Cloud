#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Cloud.Management.Api10;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// IoC module for Lokad.Cloud management classes
	/// </summary>
	public class ManagementModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register<CloudConfiguration>().As<ICloudConfigurationApi>().FactoryScoped();
			builder.Register<CloudAssemblies>().As<ICloudAssembliesApi>().FactoryScoped();
			builder.Register<CloudServices>().As<ICloudServicesApi>().FactoryScoped();
			builder.Register<CloudServiceScheduling>().As<ICloudServiceSchedulingApi>().FactoryScoped();
			builder.Register<CloudStatistics>().As<ICloudStatisticsApi>().FactoryScoped();

			// in some cases (like standalone mock storage) the RoleConfigurationSettings
			// will not be available. That's ok, since in this case Provisioning is not
			// available anyway and there's no need to make Provisioning resolveable.
			builder.Register(c => new CloudProvisioning(c.Resolve<ICloudConfigurationSettings>(), c.Resolve<ILog>()))
				.As<CloudProvisioning, IProvisioningProvider, ICloudProvisioningApi>()
				.SingletonScoped();
		}
	}
}
