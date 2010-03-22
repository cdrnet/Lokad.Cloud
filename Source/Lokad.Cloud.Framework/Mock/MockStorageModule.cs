#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Mock
{
	[NoCodeCoverage]
	public sealed class MockStorageModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register(c => new MemoryBlobStorageProvider()).As<IBlobStorageProvider>();
			builder.Register(c => new MemoryQueueStorageProvider()).As<IQueueStorageProvider>();
		}
	}
}
