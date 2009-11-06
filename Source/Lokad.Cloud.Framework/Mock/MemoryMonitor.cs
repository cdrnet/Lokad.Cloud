using System;
using Lokad.Cloud.Diagnostics;

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
