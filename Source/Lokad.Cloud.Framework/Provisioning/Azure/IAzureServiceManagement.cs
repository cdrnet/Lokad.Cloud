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
using System.ServiceModel;
using System.ServiceModel.Web;
using Lokad.Cloud.Provisioning.Azure.Entities;
using Lokad.Cloud.Provisioning.Azure.InputParameters;

// TODO: To be replaced with official REST client once available

namespace Lokad.Cloud.Provisioning.Azure
{
	/// <summary>
	/// Windows Azure Service Management API. 
	/// </summary>
	[ServiceContract(Namespace = ApiConstants.XmlNamespace)]
	internal interface IAzureServiceManagement
	{
		/// <summary>
		/// Gets the result of an asynchronous operation.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/operations/{operationTrackingId}")]
		IAsyncResult BeginGetOperationStatus(string subscriptionId, string operationTrackingId, AsyncCallback callback, object state);
		Operation EndGetOperationStatus(IAsyncResult asyncResult);

		/// <summary>
		/// Swaps the deployment to a production slot.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}")]
		IAsyncResult BeginSwapDeployment(string subscriptionId, string serviceName, SwapDeploymentInput input, AsyncCallback callback, object state);
		void EndSwapDeployment(IAsyncResult asyncResult);

		/// <summary>
		/// Creates a deployment.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}")]
		IAsyncResult BeginCreateOrUpdateDeployment(string subscriptionId, string serviceName, string deploymentSlot, CreateDeploymentInput input, AsyncCallback callback, object state);
		void EndCreateOrUpdateDeployment(IAsyncResult asyncResult);

		/// <summary>
		/// Deletes the specified deployment. This works against through the deployment name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deployments/{deploymentName}")]
		IAsyncResult BeginDeleteDeployment(string subscriptionId, string serviceName, string deploymentName, AsyncCallback callback, object state);
		void EndDeleteDeployment(IAsyncResult asyncResult);

		/// <summary>
		/// Deletes the specified deployment. This works against through the slot name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}")]
		IAsyncResult BeginDeleteDeploymentBySlot(string subscriptionId, string serviceName, string deploymentSlot, AsyncCallback callback, object state);
		void EndDeleteDeploymentBySlot(IAsyncResult asyncResult);

		/// <summary>
		/// Gets the specified deployment details.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deployments/{deploymentName}")]
		IAsyncResult BeginGetDeployment(string subscriptionId, string serviceName, string deploymentName, AsyncCallback callback, object state);
		Deployment EndGetDeployment(IAsyncResult asyncResult);

		/// <summary>
		/// Gets the specified deployment details.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}")]
		IAsyncResult BeginGetDeploymentBySlot(string subscriptionId, string serviceName, string deploymentSlot, AsyncCallback callback, object state);
		Deployment EndGetDeploymentBySlot(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates a change to the deployment. This works against through the deployment name.
		/// This is an asynchronous operation
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deployments/{deploymentName}/?comp=config")]
		IAsyncResult BeginChangeConfiguration(string subscriptionId, string serviceName, string deploymentName, ChangeConfigurationInput input, AsyncCallback callback, object state);
		void EndChangeConfiguration(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates a change to the deployment. This works against through the slot name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}/?comp=config")]
		IAsyncResult BeginChangeConfigurationBySlot(string subscriptionId, string serviceName, string deploymentSlot, ChangeConfigurationInput input, AsyncCallback callback, object state);
		void EndChangeConfigurationBySlot(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates a change to the deployment. This works against through the deployment name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deployments/{deploymentName}/?comp=status")]
		IAsyncResult BeginUpdateDeploymentStatus(string subscriptionId, string serviceName, string deploymentName, UpdateDeploymentStatusInput input, AsyncCallback callback, object state);
		void EndUpdateDeploymentStatus(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates a change to the deployment. This works against through the slot name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}/?comp=status")]
		IAsyncResult BeginUpdateDeploymentStatusBySlot(string subscriptionId, string serviceName, string deploymentSlot, UpdateDeploymentStatusInput input, AsyncCallback callback, object state);
		void EndUpdateDeploymentStatusBySlot(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates an deployment upgrade.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deployments/{deploymentName}/?comp=upgrade")]
		IAsyncResult BeginUpgradeDeployment(string subscriptionId, string serviceName, string deploymentName, UpgradeDeploymentInput input, AsyncCallback callback, object state);
		void EndUpgradeDeployment(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates an deployment upgrade through the slot name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}/?comp=upgrade")]
		IAsyncResult BeginUpgradeDeploymentBySlot(string subscriptionId, string serviceName, string deploymentSlot, UpgradeDeploymentInput input, AsyncCallback callback, object state);
		void EndUpgradeDeploymentBySlot(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates an deployment upgrade.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deployments/{deploymentName}/?comp=walkupgradedomain")]
		IAsyncResult BeginWalkUpgradeDomain(string subscriptionId, string serviceName, string deploymentName, WalkUpgradeDomainInput input, AsyncCallback callback, object state);
		void EndWalkUpgradeDomain(IAsyncResult asyncResult);

		/// <summary>
		/// Initiates an deployment upgrade through the slot name.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/deploymentslots/{deploymentSlot}/?comp=walkupgradedomain")]
		IAsyncResult BeginWalkUpgradeDomainBySlot(string subscriptionId, string serviceName, string deploymentSlot, WalkUpgradeDomainInput input, AsyncCallback callback, object state);
		void EndWalkUpgradeDomainBySlot(IAsyncResult asyncResult);

		/// <summary>
		/// Lists the affinity groups associated with the specified subscription.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/affinitygroups")]
		IAsyncResult BeginListAffinityGroups(string subscriptionId, AsyncCallback callback, object state);
		AffinityGroupList EndListAffinityGroups(IAsyncResult asyncResult);

		/// <summary>
		/// Get properties for the specified affinity group. 
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/affinitygroups/{affinityGroupName}")]
		IAsyncResult BeginGetAffinityGroup(string subscriptionId, string affinityGroupName, AsyncCallback callback, object state);
		AffinityGroup EndGetAffinityGroup(IAsyncResult asyncResult);

		/// <summary>
		/// Lists the certificates associated with a given subscription.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/certificates")]
		IAsyncResult BeginListCertificates(string subscriptionId, string serviceName, AsyncCallback callback, object state);
		CertificateList EndListCertificates(IAsyncResult asyncResult);

		/// <summary>
		/// Gets public data for the given certificate.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/certificates/{thumbprintalgorithm}-{thumbprint_in_hex}")]
		IAsyncResult BeginGetCertificate(string subscriptionId, string serviceName, string thumbprintalgorithm, string thumbprint_in_hex, AsyncCallback callback, object state);
		Certificate EndGetCertificate(IAsyncResult asyncResult);

		/// <summary>
		/// Adds certificates to the given subscription. 
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/certificates")]
		IAsyncResult BeginAddCertificates(string subscriptionId, string serviceName, CertificateFileInput input, AsyncCallback callback, object state);
		void EndAddCertificates(IAsyncResult asyncResult);

		/// <summary>
		/// Deletes the given certificate.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "DELETE", UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}/certificates/{thumbprintalgorithm}-{thumbprint_in_hex}")]
		IAsyncResult BeginDeleteCertificate(string subscriptionId, string serviceName, string thumbprintalgorithm, string thumbprint_in_hex, AsyncCallback callback, object state);
		void EndDeleteCertificate(IAsyncResult asyncResult);

		/// <summary>
		/// Lists the hosted services associated with a given subscription.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices")]
		IAsyncResult BeginListHostedServices(string subscriptionId, AsyncCallback callback, object state);
		HostedServiceList EndListHostedServices(IAsyncResult asyncResult);

		/// <summary>
		/// Gets the properties for the specified hosted service.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}")]
		IAsyncResult BeginGetHostedService(string subscriptionId, string serviceName, AsyncCallback callback, object state);
		HostedService EndGetHostedService(IAsyncResult asyncResult);

		/// <summary>
		/// Gets the detailed properties for the specified hosted service. 
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/hostedservices/{serviceName}?embed-detail={embedDetail}")]
		IAsyncResult BeginGetHostedServiceWithDetails(string subscriptionId, string serviceName, bool embedDetail, AsyncCallback callback, object state);
		HostedService EndGetHostedServiceWithDetails(IAsyncResult asyncResult);

		/// <summary>
		/// Lists the storage services associated with a given subscription.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/storageservices")]
		IAsyncResult BeginListStorageServices(string subscriptionId, AsyncCallback callback, object state);
		StorageServiceList EndListStorageServices(IAsyncResult asyncResult);

		/// <summary>
		/// Gets a storage service.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}")]
		IAsyncResult BeginGetStorageService(string subscriptionId, string serviceName, AsyncCallback callback, object state);
		StorageService EndGetStorageService(IAsyncResult asyncResult);

		/// <summary>
		/// Gets the key of a storage service.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebGet(UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}/keys")]
		IAsyncResult BeginGetStorageKeys(string subscriptionId, string serviceName, AsyncCallback callback, object state);
		StorageService EndGetStorageKeys(IAsyncResult asyncResult);

		/// <summary>
		/// Regenerates keys associated with a storage service.
		/// </summary>
		[OperationContract(AsyncPattern = true)]
		[WebInvoke(Method = "POST", UriTemplate = @"{subscriptionId}/services/storageservices/{serviceName}/keys?action=regenerate")]
		IAsyncResult BeginRegenerateStorageServiceKeys(string subscriptionId, string serviceName, RegenerateKeysInput regenerateKeys, AsyncCallback callback, object state);
		StorageService EndRegenerateStorageServiceKeys(IAsyncResult asyncResult);
	}
}