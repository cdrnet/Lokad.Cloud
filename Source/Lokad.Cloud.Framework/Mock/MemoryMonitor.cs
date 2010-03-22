using System;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Mock
{
	public class MemoryMonitor : IServiceMonitor
	{
		public IDisposable Monitor(CloudService service)
		{
			return new DisposableAction(() => { });
		}
	}
}
