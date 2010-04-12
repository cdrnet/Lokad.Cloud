#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.ServiceModel;
using System.ServiceModel.Web;

namespace Lokad.Cloud.Management.Api10
{
	[ServiceContract(Name = "CloudConfiguration", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public interface ICloudConfigurationApi
	{
		[OperationContract]
		[WebGet(UriTemplate = @"config")]
		string GetConfigurationString();

		[OperationContract]
		[WebInvoke(UriTemplate = @"config", Method = "POST")]
		void SetConfiguration(string configuration);

		[OperationContract]
		[WebInvoke(UriTemplate = @"remove", Method = "POST")]
		void RemoveConfiguration();
	}
}
