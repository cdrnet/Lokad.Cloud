#region Copyright (c) Lokad 2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Lokad.Quality;

namespace Lokad.Cloud.Web.Api10
{
	[UsedImplicitly]
	public class HybridServiceHostFactory : ServiceHostFactory 
	{
		protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
		{
			var contract = serviceType.GetInterfaces().Single(t => t.GetAttribute<ServiceContractAttribute>(false) != null);
			return new HybridServiceHost(serviceType, contract, baseAddresses);
		}
	}
}