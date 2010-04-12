#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Security;
using System.Xml;
using Microsoft.ServiceModel.Web;

namespace Lokad.Cloud.Web.Api10
{
	public class HybridServiceHost : ServiceHost
	{
		readonly Type _contractType;

		public HybridServiceHost(Type serviceType, Type contractType, params Uri[] baseAddresses)
			: base(serviceType, baseAddresses)
		{
			_contractType = contractType;
		}

		protected override void ApplyConfiguration()
		{
			base.ApplyConfiguration();

			// Debug

			var debugBehavior = Description.Behaviors.Find<ServiceDebugBehavior>();
			if (debugBehavior == null)
			{
				debugBehavior = new ServiceDebugBehavior();
				Description.Behaviors.Add(debugBehavior);
			}
			debugBehavior.HttpHelpPageEnabled = true;
			debugBehavior.HttpsHelpPageEnabled = true;
			debugBehavior.IncludeExceptionDetailInFaults = true;

			// Metadata

			var mexBehavior = Description.Behaviors.Find<ServiceMetadataBehavior>();
			if (mexBehavior == null)
			{
				mexBehavior = new ServiceMetadataBehavior();
				Description.Behaviors.Add(mexBehavior);
			}
			mexBehavior.HttpGetEnabled = true;
			mexBehavior.HttpsGetEnabled = true;

			AddServiceEndpoint(
				ServiceMetadataBehavior.MexContractName,
				MetadataExchangeBindings.CreateMexHttpBinding(),
				"mex");

			AddServiceEndpoint(
				ServiceMetadataBehavior.MexContractName,
				MetadataExchangeBindings.CreateMexHttpsBinding(),
				"mex");

			var requestHeaderMexBehavior = Description.Behaviors.Find<UseRequestHeadersForMetadataAddressBehavior>();
			if(requestHeaderMexBehavior == null)
			{
				requestHeaderMexBehavior = new UseRequestHeadersForMetadataAddressBehavior();
				Description.Behaviors.Add(requestHeaderMexBehavior);
			}

			// Credentials

			var serviceCredentials = Description.Behaviors.Find<ServiceCredentials>();
			if (serviceCredentials == null)
			{
				serviceCredentials = new ServiceCredentials();
				Description.Behaviors.Add(serviceCredentials);
			}
			serviceCredentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
			serviceCredentials.UserNameAuthentication.CustomUserNamePasswordValidator = new SoapApiKeyVerifier();
		}

		protected override void OnOpening()
		{
			// disable all endpoints if no api key was defined
			var apiKey = CloudEnvironment.GetConfigurationSetting("ManagementApiKey");
			if (!apiKey.HasValue || string.IsNullOrEmpty(apiKey.Value))
			{
				base.OnOpening();
				return;
			}

			// only enable secure endpoints if SSL is configured and enabled
			if (CloudEnvironment.HasSecureEndpoint())
			{
				// SOAP 1.1
				var soap11Binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
				soap11Binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
				AddServiceEndpoint(_contractType, soap11Binding, "");

				//// SOAP 1.2
				//var soap12Binding = new WSHttpBinding(SecurityMode.TransportWithMessageCredential);
				//soap12Binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
				//AddServiceEndpoint(_contractType, soap12Binding, "");
			}

			// REST
			var restBinding = new WebHttpBinding(WebHttpSecurityMode.None);
			restBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

			var extendedRestBinding = new CustomBinding(restBinding);
			var restInterceptors = new Collection<RequestInterceptor> { new RestApiKeyVerifier() };
			extendedRestBinding.Elements.Insert(0, new RequestInterceptorBindingElement(restInterceptors));

			var restXmlBehavior = new ExtendedWebHttpBehavior();
			var restXmlEndpoint = AddServiceEndpoint(_contractType, extendedRestBinding, "xml");
			restXmlEndpoint.Behaviors.Add(restXmlBehavior);

			//var restJsonBehavior = new WebScriptEnablingBehavior();
			//var restJsonEndpoint = AddServiceEndpoint(_contractType, restBinding, "json");
			//restJsonEndpoint.Behaviors.Add(restJsonBehavior);

			base.OnOpening();
		}

		class ExtendedWebHttpBehavior : WebHttpBehavior
		{
			protected override QueryStringConverter GetQueryStringConverter(OperationDescription operationDescription)
			{
				return new ExtendedQueryStringConverter();
			}
		}

		class ExtendedQueryStringConverter : QueryStringConverter
		{
			public override bool CanConvert(Type type)
			{
				if(type == typeof(DateTime?))
				{
					return true;
				}

				return base.CanConvert(type);
			}

			public override string ConvertValueToString(object parameter, Type parameterType)
			{
				if (parameterType == typeof(DateTime?))
				{
					return parameter == null ? null : XmlConvert.ToString((DateTime)parameter, XmlDateTimeSerializationMode.RoundtripKind);
				}

				return base.ConvertValueToString(parameter, parameterType);
			}

			public override object ConvertStringToValue(string parameter, Type parameterType)
			{
				if (parameterType == typeof(DateTime?))
				{
					return String.IsNullOrEmpty(parameter)
						? (DateTime?) null
						: DateTime.SpecifyKind(DateTime.Parse(parameter, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.Utc);
				}

				return base.ConvertStringToValue(parameter, parameterType);
			}
		}
	}
}