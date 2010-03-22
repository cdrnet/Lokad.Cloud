#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Provisioning
{
	/// <summary>Defines an interface to auto-scale your cloud app.</summary>
	/// <remarks>The implementation relies on the Management API on Windows Azure.</remarks>
	public interface IProvisioningProvider
	{
		/// <summary>Indicates where the provider is correctly setup.</summary>
		bool IsAvailable { get;  }

		/// <summary>Defines the number of regular VM instances to get allocated
		/// for the cloud app.</summary>
		/// <param name="count"></param>
		void SetWorkerInstanceCount(int count);

		/// <summary>Indicates the number of VM instances currently allocated
		/// for the cloud app.</summary>
		/// <remarks>If <see cref="IsAvailable"/> is <c>false</c> this method
		/// will be returning a <c>null</c> value.</remarks>
		Maybe<int> GetWorkerInstanceCount();
	}
}