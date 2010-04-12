#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Web;
using System.Xml.Linq;
using Microsoft.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.Net;
using System.IO;

namespace Lokad.Cloud.Web.Api10
{
	public class RestApiKeyVerifier : RequestInterceptor
	{
		readonly Maybe<string> _apiKey;

		public RestApiKeyVerifier()
			: base(false)
		{
			_apiKey = CloudEnvironment.GetConfigurationSetting("ManagementApiKey");
		}

		bool IsValidApiKey(string key)
		{
			return _apiKey.HasValue && (key == _apiKey.Value);
		}

		public override void ProcessRequest(ref RequestContext requestContext)
		{
			if (requestContext == null || requestContext.RequestMessage == null)
			{
				return;
			}

			var request = requestContext.RequestMessage;
			var requestProperty = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
			
			// try query string
			var queryParams = HttpUtility.ParseQueryString(requestProperty.QueryString);
			var querykey = queryParams["apikey"];
			if (IsValidApiKey(querykey))
			{
				return;
			}

			// try headers
			var headerKey = requestProperty.Headers["apikey"];
			if (IsValidApiKey(headerKey))
			{
				return;
			}

			// INVALID API KEY:

			var response = XElement.Load(new StringReader("<?xml version=\"1.0\" encoding=\"utf-8\"?><html xmlns=\"http://www.w3.org/1999/xhtml\" version=\"-//W3C//DTD XHTML 2.0//EN\" xml:lang=\"en\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.w3.org/1999/xhtml http://www.w3.org/MarkUp/SCHEMA/xhtml2.xsd\"><HEAD><TITLE>Request Error</TITLE></HEAD><BODY><DIV id=\"content\"><P class=\"heading1\"><B>A valid API key needs to be included using the apikey query string parameter</B></P></DIV></BODY></html>"));
			var reply = Message.CreateMessage(MessageVersion.None, null, response);
			var responseProperty = new HttpResponseMessageProperty
				{
					StatusCode = HttpStatusCode.Unauthorized,
					StatusDescription = String.Format("'{0}' is an invalid API key", querykey)
				};
			responseProperty.Headers[HttpResponseHeader.ContentType] = "text/html";
			reply.Properties[HttpResponseMessageProperty.Name] = responseProperty;
			requestContext.Reply(reply);

			// set the request context to null to terminate processing of this request
			requestContext = null;
		}
	}
}