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

namespace Lokad.Cloud.Provisioning.Azure.Entities
{
	/// <summary>
	/// Storage Service
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class StorageService : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public Uri Url { get; set; }

		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string ServiceName { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public StorageServiceProperties StorageServiceProperties { get; set; }

		[DataMember(Order = 4, EmitDefaultValue = false)]
		public StorageServiceKeys StorageServiceKeys { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// Storage Service Properties
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class StorageServiceProperties : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public string Description { get; set; }

		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string AffinityGroup { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public string Location { get; set; }

		[DataMember(Order = 4)]
		public string Label { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	///  Storage Service Keys
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class StorageServiceKeys : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public string Primary { get; set; }

		[DataMember(Order = 2)]
		public string Secondary { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// List of storage services
	/// </summary>
	[CollectionDataContract(Name = "StorageServices", ItemName = "StorageService", Namespace = ApiConstants.XmlNamespace)]
	internal class StorageServiceList : List<StorageService>
	{
		public StorageServiceList()
		{
		}

		public StorageServiceList(IEnumerable<StorageService> storageServices)
			: base(storageServices)
		{
		}
	}
}