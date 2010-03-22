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

using System.Runtime.Serialization;
using Lokad.Cloud.Provisioning.Azure.Entities;

namespace Lokad.Cloud.Provisioning.Azure.InputParameters
{
	[DataContract(Name = "UpdateDeploymentStatus", Namespace = ApiConstants.XmlNamespace)]
	internal class UpdateDeploymentStatusInput : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public DeploymentStatus Status { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}
}