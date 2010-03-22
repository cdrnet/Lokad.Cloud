#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.ServiceFabric;

namespace Lokad.Cloud.Test
{
	public sealed class GlobalSetup
	{
		static IContainer _container;

		static IContainer Setup()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new ConfigurationSettingsReader("autofac"));
			builder.RegisterModule(new RuntimeModule());

			return builder.Build();
		}

		/// <summary>Gets the IoC container as initialized by the setup.</summary>
		public static IContainer Container 
		{ 
			get
			{
				if(null == _container)
				{
					_container = Setup();
				}

				return _container;
			}
		}
	}
}