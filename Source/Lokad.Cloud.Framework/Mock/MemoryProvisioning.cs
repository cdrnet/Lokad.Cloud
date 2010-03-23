using Lokad.Cloud.Management;

namespace Lokad.Cloud.Mock
{
	public class MemoryProvisioning : IProvisioningProvider
	{
		public bool IsAvailable
		{
			get { return false; }
		}

		public void SetWorkerInstanceCount(int count)
		{
		}

		public Maybe<int> GetWorkerInstanceCount()
		{
			return Maybe<int>.Empty;
		}
	}
}
