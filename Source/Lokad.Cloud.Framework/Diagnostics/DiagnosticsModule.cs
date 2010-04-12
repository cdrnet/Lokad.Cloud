#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Cloud.Diagnostics.Persistence;
using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Cloud Diagnostics IoC Module
	/// </summary>
	[NoCodeCoverage]
	public class DiagnosticsModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			// Instrumentation
			builder.Register(c => new CloudLogger(c.Resolve<IBlobStorageProvider>(), "")).As<ILog>().DefaultOnly();
			builder.Register<ILogProvider>(c => new CloudLogProvider(c.Resolve<IBlobStorageProvider>())).DefaultOnly();

			// Cloud Monitoring
			builder.Register<BlobDiagnosticsRepository>().As<ICloudDiagnosticsRepository>().DefaultOnly();
			builder.Register<ServiceMonitor>().As<IServiceMonitor>();
			builder.Register<DiagnosticsAcquisition>()
				.OnActivating(ActivatingHandler.InjectUnsetProperties)
				.FactoryScoped();
		}
	}
}
