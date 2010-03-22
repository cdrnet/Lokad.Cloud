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

namespace Lokad.Cloud.Management.Azure
{
	internal static class ApiConstants
	{
		public const string ServiceEndpoint = "https://management.core.windows.net";
		public const string XmlNamespace = "http://schemas.microsoft.com/windowsazure";
		public const string VersionHeaderName = "x-ms-version";
		public const string OperationTrackingIdHeader = "x-ms-request-id";
		public const string VersionHeaderContent = "2009-10-01";
	}
}