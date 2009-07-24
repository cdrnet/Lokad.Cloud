#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using Lokad.Cloud.Azure;

// TODO: only most recent logs gets displayed for now.
// It's not possible (yet) to browse all logs.

namespace Lokad.Cloud.Web
{
	public partial class Logs : System.Web.UI.Page
	{
		// limiting the number of logs to be displayed
		const int MaxLogs = 20;

		readonly CloudLogger _logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

		protected void Page_Load(object sender, EventArgs e)
		{
			LogsView.DataSource = GetRecentLogs();
			LogsView.DataBind();
		}

		IEnumerable<object> GetRecentLogs()
		{
			var count = 0;
			foreach(var log in _logger.GetRecentLogs())
			{
				yield return log;
				if(MaxLogs == ++count) yield break;
			}
		}
	}
}
