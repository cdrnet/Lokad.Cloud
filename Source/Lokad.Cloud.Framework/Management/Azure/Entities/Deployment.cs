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
	/// Deployment
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class Deployment : IExtensibleDataObject
	{
		[DataMember(Order = 1, EmitDefaultValue = false)]
		public string Name { get; set; }

		[DataMember(Order = 2)]
		public DeploymentSlot DeploymentSlot { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public string PrivateID { get; set; }

		[DataMember(Order = 4)]
		public DeploymentStatus Status { get; set; }

		/// <remarks>Base64-Encoded</remarks>
		[DataMember(Order = 5, EmitDefaultValue = false)]
		public string Label { get; set; }

		[DataMember(Order = 6, EmitDefaultValue = false)]
		public Uri Url { get; set; }

		/// <remarks>Base64-Encoded</remarks>
		[DataMember(Order = 7, EmitDefaultValue = false)]
		public string Configuration { get; set; }

		[DataMember(Order = 8, EmitDefaultValue = false)]
		public RoleInstanceList RoleInstanceList { get; set; }

		[DataMember(Order = 10, EmitDefaultValue = false)]
		public UpgradeStatus UpgradeStatus { get; set; }

		[DataMember(Order = 11, EmitDefaultValue = false)]
		public int UpgradeDomainCount { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	public enum DeploymentStatus
	{
		Running,
		Suspended,
		RunningTransitioning,
		SuspendedTransitioning,
		Starting,
		Suspending,
		Deploying,
		Deleting,
	}

	public enum DeploymentSlot
	{
		Staging,
		Production,
	}

	/// <summary>
	/// Deployment Upgrade Status
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class UpgradeStatus : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public UpgradeMode UpgradeType { get; set; }

		[DataMember(Order = 2)]
		public UpgradeDomainState CurrentUpgradeDomainState { get; set; }

		[DataMember(Order = 3)]
		public int CurrentUpgradeDomain { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	public enum UpgradeMode
	{
		Auto,
		Manual
	}

	public enum UpgradeDomainState
	{
		Before,
		During
	}

	/// <summary>
	/// List of deployments
	/// </summary>
	[CollectionDataContract(Name = "Deployments", ItemName = "Deployment", Namespace = ApiConstants.XmlNamespace)]
	internal class DeploymentList : List<Deployment>
	{
		public DeploymentList()
		{
		}

		public DeploymentList(IEnumerable<Deployment> deployments)
			: base(deployments)
		{
		}
	}
}