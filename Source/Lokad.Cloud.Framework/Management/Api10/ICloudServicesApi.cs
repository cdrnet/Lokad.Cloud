#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Lokad.Cloud.Management.Api10
{
	[ServiceContract(Name = "CloudServices", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public interface ICloudServicesApi
	{
		[OperationContract]
		[WebGet(UriTemplate = @"services")]
		List<string> GetServiceNames();

		[OperationContract]
		[WebGet(UriTemplate = @"userservices")]
		List<string> GetUserServiceNames();

		[OperationContract]
		[WebGet(UriTemplate = @"services/all")]
		List<CloudServiceInfo> GetServices();

		[OperationContract]
		[WebGet(UriTemplate = @"services/{serviceName}")]
		CloudServiceInfo GetService(string serviceName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/enable", Method = "POST")]
		void EnableService(string serviceName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/disable", Method = "POST")]
		void DisableService(string serviceName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/toggle", Method = "POST")]
		void ToggleServiceState(string serviceName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/reset", Method = "POST")]
		void ResetServiceState(string serviceName);
	}
}
