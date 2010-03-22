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

// TODO: To be replaced with official REST client once available

using Lokad.Cloud.Provisioning.Azure.Entities;
using Lokad.Cloud.Provisioning.Azure.InputParameters;

namespace Lokad.Cloud.Provisioning.Azure
{
	/// <summary>
	/// Synchronous wrappers around the asynchronous web service channel.
	/// </summary>
	internal static class SynchronousApiExtensionMethods
	{
		/// <summary>
		/// Gets the result of an asynchronous operation.
		/// </summary>
		public static Operation GetOperationStatus(this IAzureServiceManagement proxy, string subscriptionId, string operationId)
		{
			return proxy.EndGetOperationStatus(proxy.BeginGetOperationStatus(subscriptionId, operationId, null, null));
		}

		/// <summary>
		/// Swaps the deployment to a production slot.
		/// </summary>
		public static void SwapDeployment(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, SwapDeploymentInput input)
		{
			proxy.EndSwapDeployment(proxy.BeginSwapDeployment(subscriptionId, serviceName, input, null, null));
		}

		/// <summary>
		/// Creates a deployment.
		/// </summary>
		public static void CreateOrUpdateDeployment(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot, CreateDeploymentInput input)
		{
			proxy.EndCreateOrUpdateDeployment(proxy.BeginCreateOrUpdateDeployment(subscriptionId, serviceName, deploymentSlot, input, null, null));
		}

		/// <summary>
		/// Deletes the specified deployment. This works against either through the deployment name.
		/// </summary>
		public static void DeleteDeployment(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentName)
		{
			proxy.EndDeleteDeployment(proxy.BeginDeleteDeployment(subscriptionId, serviceName, deploymentName, null, null));
		}

		/// <summary>
		/// Deletes the specified deployment. This works against either through the slot name.
		/// </summary>
		public static void DeleteDeploymentBySlot(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot)
		{
			proxy.EndDeleteDeploymentBySlot(proxy.BeginDeleteDeploymentBySlot(subscriptionId, serviceName, deploymentSlot, null, null));
		}

		/// <summary>
		/// Gets the specified deployment details.
		/// </summary>
		public static Deployment GetDeployment(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentName)
		{
			return proxy.EndGetDeployment(proxy.BeginGetDeployment(subscriptionId, serviceName, deploymentName, null, null));
		}

		/// <summary>
		/// Gets the specified deployment details.
		/// </summary>
		public static Deployment GetDeploymentBySlot(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot)
		{
			return proxy.EndGetDeploymentBySlot(proxy.BeginGetDeploymentBySlot(subscriptionId, serviceName, deploymentSlot, null, null));
		}

		/// <summary>
		/// Initiates a change to the deployment. This works against through the deployment name.
		/// </summary>
		public static void ChangeConfiguration(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentName, ChangeConfigurationInput input)
		{
			proxy.EndChangeConfiguration(proxy.BeginChangeConfiguration(subscriptionId, serviceName, deploymentName, input, null, null));
		}

		/// <summary>
		/// Initiates a change to the deployment. This works against through the slot name.
		/// </summary>
		public static void ChangeConfigurationBySlot(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot, ChangeConfigurationInput input)
		{
			proxy.EndChangeConfigurationBySlot(proxy.BeginChangeConfigurationBySlot(subscriptionId, serviceName, deploymentSlot, input, null, null));
		}

		/// <summary>
		/// Initiates a change to the deployment. This works against through the deployment name.
		/// </summary>
		public static void UpdateDeploymentStatus(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentName, UpdateDeploymentStatusInput input)
		{
			proxy.EndUpdateDeploymentStatus(proxy.BeginUpdateDeploymentStatus(subscriptionId, serviceName, deploymentName, input, null, null));
		}

		/// <summary>
		/// Initiates a change to the deployment. This works against through the slot name.
		/// </summary>
		public static void UpdateDeploymentStatusBySlot(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot, UpdateDeploymentStatusInput input)
		{
			proxy.EndUpdateDeploymentStatusBySlot(proxy.BeginUpdateDeploymentStatusBySlot(subscriptionId, serviceName, deploymentSlot, input, null, null));
		}

		/// <summary>
		/// Initiates an deployment upgrade.
		/// </summary>
		public static void UpgradeDeployment(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentName, UpgradeDeploymentInput input)
		{
			proxy.EndUpgradeDeployment(proxy.BeginUpgradeDeployment(subscriptionId, serviceName, deploymentName, input, null, null));
		}

		/// <summary>
		/// Initiates an deployment upgrade through the slot name.
		/// </summary>
		public static void UpgradeDeploymentBySlot(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot, UpgradeDeploymentInput input)
		{
			proxy.EndUpgradeDeploymentBySlot(proxy.BeginUpgradeDeploymentBySlot(subscriptionId, serviceName, deploymentSlot, input, null, null));
		}

		/// <summary>
		/// Initiates an deployment upgrade.
		/// </summary>
		public static void WalkUpgradeDomain(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentName, WalkUpgradeDomainInput input)
		{
			proxy.EndWalkUpgradeDomain(proxy.BeginWalkUpgradeDomain(subscriptionId, serviceName, deploymentName, input, null, null));
		}

		/// <summary>
		/// Initiates an deployment upgrade through the slot name.
		/// </summary>
		public static void WalkUpgradeDomainBySlot(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string deploymentSlot, WalkUpgradeDomainInput input)
		{
			proxy.EndWalkUpgradeDomainBySlot(proxy.BeginWalkUpgradeDomainBySlot(subscriptionId, serviceName, deploymentSlot, input, null, null));
		}

		/// <summary>
		/// Lists the affinity groups associated with the specified subscription.
		/// </summary>
		public static AffinityGroupList ListAffinityGroups(this IAzureServiceManagement proxy, string subscriptionId)
		{
			return proxy.EndListAffinityGroups(proxy.BeginListAffinityGroups(subscriptionId, null, null));
		}

		/// <summary>
		/// Get properties for the specified affinity group. 
		/// </summary>
		public static AffinityGroup GetAffinityGroup(this IAzureServiceManagement proxy, string subscriptionId, string affinityGroupName)
		{
			return proxy.EndGetAffinityGroup(proxy.BeginGetAffinityGroup(subscriptionId, affinityGroupName, null, null));
		}

		/// <summary>
		/// Lists the certificates associated with a given subscription.
		/// </summary>
		public static CertificateList ListCertificates(this IAzureServiceManagement proxy, string subscriptionId, string serviceName)
		{
			return proxy.EndListCertificates(proxy.BeginListCertificates(subscriptionId, serviceName, null, null));
		}

		/// <summary>
		/// Gets public data for the given certificate.
		/// </summary>
		public static Certificate GetCertificate(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string algorithm, string thumbprint)
		{
			return proxy.EndGetCertificate(proxy.BeginGetCertificate(subscriptionId, serviceName, algorithm, thumbprint, null, null));
		}

		/// <summary>
		/// Adds certificates to the given subscription. 
		/// </summary>
		public static void AddCertificates(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, CertificateFileInput input)
		{
			proxy.EndAddCertificates(proxy.BeginAddCertificates(subscriptionId, serviceName, input, null, null));
		}

		/// <summary>
		/// Deletes the given certificate.
		/// </summary>
		public static void DeleteCertificate(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, string algorithm, string thumbprint)
		{
			proxy.EndDeleteCertificate(proxy.BeginDeleteCertificate(subscriptionId, serviceName, algorithm, thumbprint, null, null));
		}

		/// <summary>
		/// Lists the hosted services associated with a given subscription.
		/// </summary>
		public static HostedServiceList ListHostedServices(this IAzureServiceManagement proxy, string subscriptionId)
		{
			return proxy.EndListHostedServices(proxy.BeginListHostedServices(subscriptionId, null, null));
		}

		/// <summary>
		/// Gets the properties for the specified hosted service.
		/// </summary>
		public static HostedService GetHostedService(this IAzureServiceManagement proxy, string subscriptionId, string serviceName)
		{
			return proxy.EndGetHostedService(proxy.BeginGetHostedService(subscriptionId, serviceName, null, null));
		}

		/// <summary>
		/// Gets the detailed properties for the specified hosted service. 
		/// </summary>
		public static HostedService GetHostedServiceWithDetails(this IAzureServiceManagement proxy, string subscriptionId, string serviceName, bool embedDetail)
		{
			return proxy.EndGetHostedServiceWithDetails(proxy.BeginGetHostedServiceWithDetails(subscriptionId, serviceName, embedDetail, null, null));
		}

		/// <summary>
		/// Lists the storage services associated with a given subscription.
		/// </summary>
		public static StorageServiceList ListStorageServices(this IAzureServiceManagement proxy, string subscriptionId)
		{
			return proxy.EndListStorageServices(proxy.BeginListStorageServices(subscriptionId, null, null));
		}

		/// <summary>
		/// Gets a storage service.
		/// </summary>
		public static StorageService GetStorageService(this IAzureServiceManagement proxy, string subscriptionId, string name)
		{
			return proxy.EndGetStorageService(proxy.BeginGetStorageService(subscriptionId, name, null, null));
		}

		/// <summary>
		/// Gets the key of a storage service.
		/// </summary>
		public static StorageService GetStorageKeys(this IAzureServiceManagement proxy, string subscriptionId, string name)
		{
			return proxy.EndGetStorageKeys(proxy.BeginGetStorageKeys(subscriptionId, name, null, null));
		}

		/// <summary>
		/// Regenerates keys associated with a storage service.
		/// </summary>
		public static StorageService RegenerateStorageServiceKeys(this IAzureServiceManagement proxy, string subscriptionId, string name, RegenerateKeysInput regenerateKeys)
		{
			return proxy.EndRegenerateStorageServiceKeys(proxy.BeginRegenerateStorageServiceKeys(subscriptionId, name, regenerateKeys, null, null));
		}
	}
}