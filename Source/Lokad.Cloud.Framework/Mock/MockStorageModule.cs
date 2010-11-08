#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Storage.InMemory;
using Lokad.Quality;

namespace Lokad.Cloud.Mock
{
	[NoCodeCoverage]
	public sealed class MockStorageModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			// From Lokad.Cloud.Storage
			builder.Register(c => new MemoryBlobStorageProvider()).As<IBlobStorageProvider>();
			builder.Register(c => new MemoryQueueStorageProvider()).As<IQueueStorageProvider>();
			builder.Register(c => new MemoryTableStorageProvider()).As<ITableStorageProvider>();

			builder.Register(c => new MemoryLogger()).As<ILog>();
			builder.Register(c => new MemoryMonitor()).As<IServiceMonitor>();
			builder.Register(c => new MemoryProvisioning()).As<IProvisioningProvider>();
		}
	}
}
