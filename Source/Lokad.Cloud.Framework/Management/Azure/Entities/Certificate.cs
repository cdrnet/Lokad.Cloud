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
	/// Certificate
	/// </summary>
	[DataContract(Namespace = ApiConstants.XmlNamespace)]
	internal class Certificate : IExtensibleDataObject
	{
		[DataMember(Order = 1, EmitDefaultValue = false)]
		public Uri CertificateUrl { get; set; }

		[DataMember(Order = 2, EmitDefaultValue = false)]
		public string Thumbprint { get; set; }

		[DataMember(Order = 3, EmitDefaultValue = false)]
		public string ThumbprintAlgorithm { get; set; }

		/// <remarks>Base64-Encoded X509</remarks>
		[DataMember(Order = 4, EmitDefaultValue = false)]
		public string Data { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}

	/// <summary>
	/// List of certificates
	/// </summary>
	[CollectionDataContract(Name = "Certificates", ItemName = "Certificate", Namespace = ApiConstants.XmlNamespace)]
	internal class CertificateList : List<Certificate>
	{
		public CertificateList()
		{
		}

		public CertificateList(IEnumerable<Certificate> certificateList)
			: base(certificateList)
		{
		}
	}
}