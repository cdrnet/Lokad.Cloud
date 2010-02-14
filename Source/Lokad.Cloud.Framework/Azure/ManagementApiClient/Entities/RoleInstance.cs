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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lokad.Cloud.Azure.ManagementApiClient
{
	/// <summary>
	/// Role Instance
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class RoleInstance
	{
		[DataMember(Order = 1)]
		public string RoleName { get; set; }

		[DataMember(Order = 2)]
		public string InstanceName { get; set; }

		[DataMember(Order = 3)]
		public RoleInstanceStatus InstanceStatus { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	public enum RoleInstanceStatus
	{
		Initializing,
		Ready,
		Busy,
		Stopping,
		Stopped,
		Unresponsive
	}

	/// <summary>
	/// List of role instances
	/// </summary>
	[CollectionDataContract(Name = "RoleInstanceList", ItemName = "RoleInstance", Namespace = ApiConstants.XmlNamespace)]
	internal class RoleInstanceList : List<RoleInstance>
	{
		public RoleInstanceList()
		{
		}

		public RoleInstanceList(IEnumerable<RoleInstance> roles)
			: base(roles)
		{
		}
	}
}
