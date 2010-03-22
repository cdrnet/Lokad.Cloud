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

namespace Lokad.Cloud.Provisioning.Azure.Entities
{
	/// <summary>
	/// Asynchronous Operation
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class Operation : IExtensibleDataObject
	{
		[DataMember(Name = "ID", Order = 1)]
		public string OperationTrackingId { get; set; }

		[DataMember(Order = 2)]
		public OperationStatus Status { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public int HttpStatusCode { get; set; }

		[DataMember(Order = 4, EmitDefaultValue = false)]
		public OperationError Error { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	public enum OperationStatus
	{
		InProgress,
		Succeeded,
		Failed,
	}

	/// <summary>
	/// Asynchronous Operation Error
	/// </summary>
	[DataContract(Name = "Error", Namespace = ApiConstants.XmlNamespace)]
	internal class OperationError : IExtensibleDataObject
	{
		[DataMember(Order = 1)]
		public OperationErrorCode Code { get; set; }

		[DataMember(Order = 2)]
		public string Message { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	public enum OperationErrorCode
	{
		MissingOrIncorrectVersionHeader,
		InvalidRequest,
		InvalidXmlRequest,
		InvalidContentType,
		MissingOrInvalidRequiredQueryParameter,
		InvalidHttpVerb,
		InternalError,
		BadRequest,
		AuthenticationFailed,
		ResourceNotFound,
		SubscriptionDisabled,
		ServerBusy,
		TooManyRequests,
		ConflictError,
	}
}