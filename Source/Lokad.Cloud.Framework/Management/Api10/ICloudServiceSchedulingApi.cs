#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Lokad.Cloud.Management.Api10
{
	[ServiceContract(Name = "CloudServiceScheduling", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public interface ICloudServiceSchedulingApi
	{
		[OperationContract]
		[WebGet(UriTemplate = @"services")]
		List<string> GetScheduledServiceNames();

		[OperationContract]
		[WebGet(UriTemplate = @"userservices")]
		List<string> GetScheduledUserServiceNames();

		[OperationContract]
		[WebGet(UriTemplate = @"services/all")]
		List<CloudServiceSchedulingInfo> GetSchedules();

		[OperationContract]
		[WebGet(UriTemplate = @"services/{serviceName}")]
		CloudServiceSchedulingInfo GetSchedule(string serviceName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/interval?timespan={triggerInterval}", Method = "POST")]
		void SetTriggerInterval(string serviceName, TimeSpan triggerInterval);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/reset", Method = "POST")]
		void ResetSchedule(string serviceName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"services/{serviceName}/release", Method = "POST")]
		void ReleaseLease(string serviceName);
	}
}
