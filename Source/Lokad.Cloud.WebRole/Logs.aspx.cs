#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Web.Caching;
using Lokad.Cloud.Azure;

// TODO: only most recent logs gets displayed for now.
// It's not possible (yet) to browse all logs.

namespace Lokad.Cloud.Web
{
	public partial class Logs : System.Web.UI.Page
	{
		const string LogsCacheKey = "lokad-cloud-logs";

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
			// HACK: cache logic is completely custom and would probably need to
			// be abstracted away.

			var cachedLogs = Cache[LogsCacheKey] as List<LogEntry> ?? new List<LogEntry>();

			var newLogs = new List<LogEntry>();

			// Retrieving logs from the blob storage until the cache catch-up.
			var count = 0;
			foreach(var log in _logger.GetRecentLogs())
			{
				if (cachedLogs.Count > 0 && cachedLogs[0].DateTime == log.DateTime)
				{
					break; // retrieve the logs for the cache instead
				}

				// adding new entries to cache
				if(cachedLogs.Count == 0 || cachedLogs[0].DateTime != log.DateTime)
				{
					newLogs.Add(log);
				}

				yield return log;
				if(MaxLogs == ++count) break;
			}

			// Retrieving logs from the cache. 
			for(int i = 0; i < cachedLogs.Count && count < MaxLogs; i++)
			{
				var log = cachedLogs[i];

				yield return log;
				if (MaxLogs == ++count) break;
			}

			// Merging existing logs and newly retrieved ones.
			cachedLogs.InsertRange(0, newLogs);

			// Saving cache for 7 days
			Cache.Add(LogsCacheKey, cachedLogs, null, 
				Cache.NoAbsoluteExpiration, new TimeSpan(7, 0, 0, 0),
				CacheItemPriority.Default, null);
		}
	}
}
