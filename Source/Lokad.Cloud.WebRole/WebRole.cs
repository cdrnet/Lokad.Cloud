
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;

namespace Lokad.Cloud.Web
{
	public class WebRole : RoleEntryPoint
	{
		public override bool OnStart()
		{
			return base.OnStart();
		}
	}
}
