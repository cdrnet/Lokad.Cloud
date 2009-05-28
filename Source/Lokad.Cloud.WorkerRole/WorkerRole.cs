#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud
{
	public class WorkerRole : RoleEntryPoint
	{
		public override void Start()
		{
			// This is a sample worker implementation. Replace with your logic.
			RoleManager.WriteToLog("Information", "Worker Process entry point called");

			while (true)
			{
				Thread.Sleep(10000);
				RoleManager.WriteToLog("Information", "Working");
			}
		}

		public override RoleStatus GetHealthStatus()
		{
			// This is a sample worker implementation. Replace with your logic.
			return RoleStatus.Healthy;
		}
	}
}
