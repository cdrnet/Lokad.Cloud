#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Management;
using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Web.Api10
{
	[UsedImplicitly]
	public class CloudAssembliesAdapter : CloudAssemblies
	{
		public CloudAssembliesAdapter()
			: base(GlobalSetup.Container.Resolve<IBlobStorageProvider>(),
			GlobalSetup.Container.Resolve<ILog>())
		{
		}
	}
}
