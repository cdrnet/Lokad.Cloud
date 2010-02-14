#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud
{
	public interface IProvisioningProvider
	{
		bool IsAvailable { get;  }
		void SetWorkerInstanceCount(int count);
		Maybe<int> GetWorkerInstanceCount();
	}
}
