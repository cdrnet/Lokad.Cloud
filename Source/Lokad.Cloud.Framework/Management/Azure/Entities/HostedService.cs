#region Copyright (c) Lokad 2010, Microsoft
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
//
// Based on Microsoft Sample Code from http://code.msdn.microsoft.com/azurecmdlets
//---------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.  
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
//---------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Management.Azure.Entities
{
	/// <summary>
	/// Hosted Service
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class HostedService : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public Uri Url { get; set; }

		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string ServiceName { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public HostedServiceProperties HostedServiceProperties { get; set; }

		[DataMember(Order = 4, EmitDefaultValue = false)]
		public DeploymentList Deployments { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// Hosted Service Properties
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class HostedServiceProperties : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public string Description { get; set; }

		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string AffinityGroup { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public string Location { get; set; }

		/// <remarks>Base64-Encoded</remarks>
		[DataMember(Order = 4)]
		public string Label { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// List of host services
	/// </summary>
	[CollectionDataContract(Name = "HostedServices", ItemName = "HostedService", Namespace = ApiConstants.XmlNamespace)]
	internal class HostedServiceList : List<HostedService>
	{
		public HostedServiceList()
		{
		}

		public HostedServiceList(IEnumerable<HostedService> hostedServices)
			: base(hostedServices)
		{
		}
	}
}