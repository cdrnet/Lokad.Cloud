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

namespace Lokad.Cloud.Management.Azure.Entities
{
	/// <summary>
	/// Affinity Group
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class AffinityGroup : IExtensibleDataObject
	{
		[DataMember(Order = 1, EmitDefaultValue = false)]
		public string Name { get; set; }

		/// <remarks>Base64-Encoded</remarks>
		[DataMember(Order = 2)]
		public string Label { get; set; }

		[DataMember(Order = 3)]
		public string Description { get; set; }

		[DataMember(Order = 4)]
		public string Location { get; set; }

		[DataMember(Order = 5, EmitDefaultValue = false)]
		public HostedServiceList HostedServices { get; set; }

		[DataMember(Order = 6, EmitDefaultValue = false)]
		public StorageServiceList StorageServices { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// List of affinity groups
	/// </summary>
	[CollectionDataContract(Name = "AffinityGroups", ItemName = "AffinityGroup", Namespace = ApiConstants.XmlNamespace)]
	internal class AffinityGroupList : List<AffinityGroup>
	{
		public AffinityGroupList()
		{
		}

		public AffinityGroupList(IEnumerable<AffinityGroup> affinityGroups)
			: base(affinityGroups)
		{
		}
	}
}