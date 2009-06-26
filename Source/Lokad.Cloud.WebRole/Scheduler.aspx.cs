#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Lokad.Cloud.Core;
using Lokad.Cloud.Framework;

namespace Lokad.Cloud.Web
{
	public partial class Scheduler : System.Web.UI.Page
	{
		readonly IBlobStorageProvider _provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();

		protected void Page_Load(object sender, EventArgs e)
		{

		}

		IEnumerable<object> GetSchedules()
		{
			var cn = ScheduledService.ContainerName;

			throw new NotImplementedException();
		}
	}
}
