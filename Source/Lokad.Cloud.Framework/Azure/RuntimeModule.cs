#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;
using Lokad.Quality;
using Module = Autofac.Builder.Module;

namespace Lokad.Cloud.Azure
{
	/// <summary>Loads all the modules required to setup a worker runtime,
	/// with <see cref="StorageModule"/>, <see cref="ProvisioningModule"/>
	/// and <see cref="DiagnosticsModule"/> among others.</summary>
	[NoCodeCoverage]
	public class RuntimeModule : Module
	{
		/// <summary>Optional provisioning settings.</summary>
		public Maybe<RoleConfigurationSettings> RoleConfiguration { get; set;}

		/// <summary>IoC constructor.</summary>
		public RuntimeModule()
		{
			RoleConfiguration = Maybe<RoleConfigurationSettings>.Empty;
		}

		protected override void Load(ContainerBuilder builder)
		{
			// azure specific
			builder.RegisterModule(new ProvisioningModule(RoleConfiguration));

			// logger and statistics
			builder.RegisterModule(new DiagnosticsModule());

			// runtime specific
			builder.Register(typeof(RuntimeFinalizer)).As<IRuntimeFinalizer>();
		}
	}
}
