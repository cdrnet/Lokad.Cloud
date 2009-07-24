#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Xml.XPath;
using Lokad.Cloud.Core;
using Microsoft.Samples.ServiceHosting.StorageClient;

// TODO: [vermorel] This class will most probably be reimplemented using Azure logger instead
// http://davidaiken.com/windows-azure/windows-azure-online-log-reader/
// For now, we are custom a custom logging framework, because azure logs can't be automatically
// retrieved, and need to be manually managed through the Azure console WebUI.

namespace Lokad.Cloud.Azure
{
	/// <summary>Log entry (when retrieving logs with the <see cref="CloudLogger"/>.
	/// </summary>
	public class LogEntry
	{
		public DateTime DateTime { get; set; }
		public string Level { get; set; }
		public string Message { get; set; }
		public string Error { get; set; }
	}

	/// <summary>Logger built on top of the Blob Storage.</summary>
	/// <remarks>
	/// Logs are formatted in XML with
	/// <code>
	/// &lt;log&gt;
	///   &lt;message&gt; {0} &lt;/message&gt;
	///   &lt;error&gt; {1} &lt;/error&gt;
	/// &lt;/log&gt;
	/// </code>
	/// 
	/// Also, the logger is relying on date prefix in order to facilitate large
	/// scale enumeration of the logs. Yet, in order to facilitate fast enumeration
	/// of recent logs, an prefix inversion trick is used.
	/// </remarks>
	public class CloudLogger : ILog
	{
		public const string ContainerName = "lokad-cloud-logs";
		public const string Delimiter = "/";

		IBlobStorageProvider _provider;
		LogLevel _logLevelThreshold;

		/// <summary>Minimal log level (inclusive), below this level,
		/// notifications are ignored.</summary>
		public LogLevel LogLevelThreshold
		{
			get { return _logLevelThreshold;  }
			set { _logLevelThreshold = value; }
		}

		public CloudLogger(IBlobStorageProvider provider)
		{
			_provider = provider;
			_logLevelThreshold = LogLevel.Min;
		}

		public void Log(LogLevel level, object message)
		{
			Log(level, null, message);
		}

		public void Log(LogLevel level, Exception ex, object message)
		{
			if (!IsEnabled(level)) return;

			var blobName = GetNewLogBlobName(level);

			var log = string.Format(@"
<log>
  <message>{0}</message>
  <error>{1}</error>
</log>
", SecurityElement.Escape(message.ToString()),
   SecurityElement.Escape(ex.ToString()) ?? string.Empty);


			// on first execution, container need to be created.
			var policy = ActionPolicy.With(e =>
				{
					var storageException = e as StorageClientException;
					if(storageException == null) return false;
					return storageException.ErrorCode == StorageErrorCode.ContainerNotFound;
				})
				.Retry(2, (e, i) => _provider.CreateContainer(ContainerName));
			
			policy.Do(() =>
				{
					var attempt = 0;
					while (!_provider.PutBlob(ContainerName, blobName + attempt, log, false))
					{
						attempt++;
					}
				});
		}

		public bool IsEnabled(LogLevel level)
		{
			return level >= _logLevelThreshold;
		}

		/// <summary>Lazily enumerates over the entire logs.</summary>
		/// <returns></returns>
		public IEnumerable<LogEntry> GetRecentLogs()
		{
			foreach(var blobName in _provider.List(ContainerName, string.Empty))
			{
				var prefix = blobName.Substring(0, 23); // prefix is always 23 char long
				var dateTime = ToDateTime(prefix);

				var level = blobName.Substring(23).Split(new []{'/'}, StringSplitOptions.RemoveEmptyEntries)[0];


				var rawlog = _provider.GetBlob<string>(ContainerName, blobName);

				string message;
				string error;
				using (var stream = new StringReader(rawlog))
				{
					var xpath = new XPathDocument(stream);
					var nav = xpath.CreateNavigator();
					message = nav.SelectSingleNode("/log/message").InnerXml;
					error = nav.SelectSingleNode("/log/error").InnerXml;
				}

				yield return new LogEntry
					{
						DateTime = dateTime,
						Level = level,
						Message = message,
						Error = error
					};
			}
		}

		static string GetNewLogBlobName(LogLevel level)
		{
			var builder = new StringBuilder();
			builder.Append(ToPrefix(DateTime.Now));
			builder.Append(Delimiter);
			builder.Append(level.ToString());
			builder.Append(Delimiter);

			return builder.ToString();
		}

		/// <summary>Time prefix with inversion in order to enumerate
		/// starting from the most recent.</summary>
		/// <remarks>This method is the symmetric of <see cref="ToDateTime"/>.</remarks>
		public static string ToPrefix(DateTime dateTime)
		{
			dateTime = dateTime.ToUniversalTime();

			// yyyy/MM/dd/hh/mm/ss/fff
			return string.Format("{0}/{1}/{2}/{3}/{4}/{5}/{6}",
				(10000 - dateTime.Year).ToString(CultureInfo.InvariantCulture),
				(12 - dateTime.Month).ToString("00"),
				(31 - dateTime.Day).ToString("00"),
				(24 - dateTime.Hour).ToString("00"),
				(60 - dateTime.Minute).ToString("00"),
				(60 - dateTime.Second).ToString("00"),
				(999 - dateTime.Millisecond).ToString("000"));
		}

		/// <summary>Convert a prefix with inversion into a <c>DateTime</c>.</summary>
		/// <remarks>This method is the symmetric of <see cref="ToPrefix"/>.</remarks>
		public static DateTime ToDateTime(string prefix)
		{
			var tokens = prefix.Split('/');

			if(tokens.Length != 7) throw new ArgumentException("Incorrect prefix.", "prefix");

			var year = 10000 - int.Parse(tokens[0], CultureInfo.InvariantCulture);
			var month = 12 - int.Parse(tokens[1], CultureInfo.InvariantCulture);
			var day = 31 - int.Parse(tokens[2], CultureInfo.InvariantCulture);
			var hour = 24 - int.Parse(tokens[3], CultureInfo.InvariantCulture);
			var minute = 60 - int.Parse(tokens[4], CultureInfo.InvariantCulture);
			var second = 60 - int.Parse(tokens[5], CultureInfo.InvariantCulture);
			var millisecond = 999 - int.Parse(tokens[6], CultureInfo.InvariantCulture);

			return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
		}
	}
}
