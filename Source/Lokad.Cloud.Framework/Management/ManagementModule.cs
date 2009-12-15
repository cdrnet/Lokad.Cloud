#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;

namespace Lokad.Cloud.Management
{
	/// <summary>
	/// IoC module for Lokad.Cloud management classes
	/// </summary>
	public class ManagementModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register<CloudConfiguration>().FactoryScoped();
			builder.Register<CloudAssemblies>().FactoryScoped();
			builder.Register<CloudServices>().FactoryScoped();
			builder.Register<CloudServiceScheduling>().FactoryScoped();
			builder.Register<CloudStatistics>().FactoryScoped();
		}
	}
}
