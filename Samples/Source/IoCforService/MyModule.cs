#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Autofac.Builder;
using Lokad;

namespace IoCforService
{
	class MyModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register(c => new MyProvider(c.Resolve<ILog>()));
		}
	}
}
