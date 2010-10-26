#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using Lokad.Cloud.ServiceFabric;

namespace IoCforService
{
	/// <summary>Sample service is IoC populated property.</summary>
	public class MyService : ScheduledService
	{
		/// <summary>IoC populated property. </summary>
		public MyProvider Provider { get; set; }

		protected override void StartOnSchedule()
		{
			if(null == Providers)
			{
				throw new InvalidOperationException("Provider should have been populated.");
			}
		}
	}
}
