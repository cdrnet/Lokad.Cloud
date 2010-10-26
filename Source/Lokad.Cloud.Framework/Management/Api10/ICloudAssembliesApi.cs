#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Lokad.Cloud.Management.Api10
{
	[ServiceContract(Name = "CloudAssemblies", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public interface ICloudAssembliesApi
	{
		[OperationContract]
		[WebGet(UriTemplate = @"assemblies")]
		List<CloudAssemblyInfo> GetAssemblies();

		[OperationContract]
		[WebInvoke(UriTemplate = @"upload/dll/{filename}", Method = "POST")]
		void UploadAssemblyDll(byte[] data, string fileName);

		[OperationContract]
		[WebInvoke(UriTemplate = @"upload/zip", Method = "POST")]
		void UploadAssemblyZipContainer(byte[] data);

		[OperationContract]
		[WebInvoke(UriTemplate = @"upload/zip/isvalid", Method = "POST")]
		bool IsValidZipContainer(byte[] data);
	}
}
