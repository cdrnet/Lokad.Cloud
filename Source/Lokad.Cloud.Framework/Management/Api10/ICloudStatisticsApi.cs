#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using Lokad.Cloud.Diagnostics;

namespace Lokad.Cloud.Management.Api10
{
	[ServiceContract(Name = "CloudStatistics", Namespace = "http://schemas.lokad.com/lokad-cloud/management/1.0")]
	public interface ICloudStatisticsApi
	{
		[OperationContract]
		[WebGet(UriTemplate = @"partitions/month?date={monthUtc}")]
		List<PartitionStatistics> GetPartitionsOfMonth(DateTime? monthUtc);

		[OperationContract]
		[WebGet(UriTemplate = @"partitions/day?date={dayUtc}")]
		List<PartitionStatistics> GetPartitionsOfDay(DateTime? dayUtc);

		[OperationContract]
		[WebGet(UriTemplate = @"services/month?date={monthUtc}")]
		List<ServiceStatistics> GetServicesOfMonth(DateTime? monthUtc);

		[OperationContract]
		[WebGet(UriTemplate = @"services/day?date={dayUtc}")]
		List<ServiceStatistics> GetServicesOfDay(DateTime? dayUtc);

		[OperationContract]
		[WebGet(UriTemplate = @"profiles/month?date={monthUtc}")]
		List<ExecutionProfilingStatistics> GetProfilesOfMonth(DateTime? monthUtc);

		[OperationContract]
		[WebGet(UriTemplate = @"profiles/day?date={dayUtc}")]
		List<ExecutionProfilingStatistics> GetProfilesOfDay(DateTime? dayUtc);
	}
}