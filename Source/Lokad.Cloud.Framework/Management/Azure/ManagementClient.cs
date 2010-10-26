#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

// TODO: To be replaced with official REST client once available

namespace Lokad.Cloud.Management.Azure
{
	internal class ManagementClient : IDisposable
	{
		readonly X509Certificate2 _certificate;
		readonly object _sync = new object();

		WebChannelFactory<IAzureServiceManagement> _factory;

		public ManagementClient(X509Certificate2 certificate)
		{
			if (certificate == null)
			{
				throw new ArgumentNullException("certificate");
			}

			_certificate = certificate;
		}

		public IAzureServiceManagement CreateChannel()
		{
			// long lock, but should in practice never be accessed
			// from two threads anyway (just a safeguard) and this way
			// we avoid multiple sync monitor enters per call
			lock (_sync)
			{
				if (_factory != null)
				{
					switch (_factory.State)
					{
						case CommunicationState.Closed:
						case CommunicationState.Closing:
							// TODO: consider reusing the factory
							_factory = null;
							break;
						case CommunicationState.Faulted:
							_factory.Close();
							_factory = null;
							break;
					}
				}

				if (_factory == null)
				{
					var binding = new WebHttpBinding(WebHttpSecurityMode.Transport);
					binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;

					_factory = new WebChannelFactory<IAzureServiceManagement>(binding, new Uri(ApiConstants.ServiceEndpoint));
					_factory.Endpoint.Behaviors.Add(new ClientOutputMessageInspector());
					_factory.Credentials.ClientCertificate.Certificate = _certificate;
				}

				return _factory.CreateChannel();
			}
		}

		public void Dispose()
		{
			lock (_sync)
			{
				if (_factory == null)
				{
					return;
				}

				_factory.Close();
				_factory = null;
			}
		}

		public void CloseChannel(IAzureServiceManagement channel)
		{
			var clientChannel = channel as IClientChannel;
			if (clientChannel != null)
			{
				clientChannel.Close();
				clientChannel.Dispose();
			}
		}

		private class ClientOutputMessageInspector : IClientMessageInspector, IEndpointBehavior
		{
			public void AfterReceiveReply(ref Message reply, object correlationState)
			{
			}

			public object BeforeSendRequest(ref Message request, IClientChannel channel)
			{
				var property = (HttpRequestMessageProperty)request.Properties[HttpRequestMessageProperty.Name];
				property.Headers.Add(ApiConstants.VersionHeaderName, ApiConstants.VersionHeaderContent);
				return null;
			}

			public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
			{
			}

			public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
			{
				clientRuntime.MessageInspectors.Add(this);
			}

			public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
			{
			}

			public void Validate(ServiceEndpoint endpoint)
			{
			}
		}
	}
}