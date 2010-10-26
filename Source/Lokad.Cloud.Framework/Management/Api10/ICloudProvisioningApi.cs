#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.ServiceModel;
using System.ServiceModel.Web;

namespace Lokad.Cloud.Management.Api10
{
	[ServiceContract(Name = "CloudProvisioning", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public interface ICloudProvisioningApi
	{
		[OperationContract]
		[WebGet(UriTemplate = @"instances")]
		int GetWorkerInstanceCount();

		[OperationContract]
		[WebInvoke(UriTemplate = @"instances/set?newcount={instanceCount}", Method = "POST")]
		void SetWorkerInstanceCount(int instanceCount);
	}
}
