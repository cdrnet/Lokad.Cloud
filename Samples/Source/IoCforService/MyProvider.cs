#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using Lokad;

namespace IoCforService
{
	/// <summary>Sample provider, registered though Autofac module.</summary>
	public class MyProvider
	{
		public MyProvider(ILog logger)
		{
			logger.Log(LogLevel.Info, "Client IoC module loaded.");
		}
	}
}
