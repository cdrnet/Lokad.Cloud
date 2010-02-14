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

namespace Lokad.Cloud.Azure.ManagementApiClient
{
	[DataContract(Name = "WalkUpgradeDomain", Namespace = ApiConstants.XmlNamespace)]
	internal class WalkUpgradeDomainInput : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public int UpgradeDomain { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}
}
