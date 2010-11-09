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
using Lokad.Cloud.Storage.Blobs;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>Log entry (when retrieving logs with the <see cref="CloudLogger"/>.
	/// </summary>
	public class LogEntry
	{
		public DateTime DateTime { get; set; }
		public string Level { get; set; }
		public string Message { get; set; }
		public string Error { get; set; }
		public string Source { get; set; }
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
		public const int DeleteBatchSize = 50;

		private static readonly char[] DelimiterCharArray = Delimiter.ToCharArray();


		readonly IBlobStorageProvider _provider;
		readonly string _source;
		LogLevel _logLevelThreshold;

		/// <summary>Minimal log level (inclusive), below this level,
		/// notifications are ignored.</summary>
		public LogLevel LogLevelThreshold
		{
			get { return _logLevelThreshold;  }
			set { _logLevelThreshold = value; }
		}

		public CloudLogger(IBlobStorageProvider provider, string source)
		{
			_provider = provider;
			_source = source;
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

			var log = string.Format(
				@"
<log>
  <message>{0}</message>
  <error>{1}</error>
  <source>{2}</source>
</log>
",
				SecurityElement.Escape(message.ToString()),
				ex != null ? SecurityElement.Escape(ex.ToString()) : string.Empty,
				string.IsNullOrEmpty(_source) ? "" : SecurityElement.Escape(_source));


			// on first execution, container needs to be created.
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

		private static string GetNamePrefix(string blobName)
		{
			return blobName.Substring(0, 23); // prefix is always 23 char long
		}

		private static LogEntry DecodeLogEntry(string blobName, string blobContent)
		{
			var prefix = GetNamePrefix(blobName);
			var dateTime = ToDateTime(prefix);

			var level = blobName.Substring(23).Split(DelimiterCharArray, StringSplitOptions.RemoveEmptyEntries)[0];
			
			using(var stream = new StringReader(blobContent))
			{
				var xpath = new XPathDocument(stream);
				var nav = xpath.CreateNavigator();

				return new LogEntry
					{
						DateTime = dateTime,
						Level = level,
						Message = nav.SelectSingleNode("/log/message").InnerXml,
						Error = nav.SelectSingleNode("/log/error").InnerXml,
						Source = nav.SelectSingleNode("/log/source").InnerXml,
					};
			}
		}

		/// <summary>Lazily enumerates over the entire logs.</summary>
		/// <returns></returns>
		public IEnumerable<LogEntry> GetRecentLogs()
		{
			foreach(var blobName in _provider.List(ContainerName, string.Empty))
			{
				var rawlog = _provider.GetBlob<string>(ContainerName, blobName);
				if (!rawlog.HasValue)
				{
					continue;
				}

				yield return DecodeLogEntry(blobName, rawlog.Value);
			}
		}

		/// <summary>Lazily loads a page of logs.</summary>
		/// <param name="pageIndex">The zero-based index of the page.</param>
		/// <param name="pageSize">The size of the page.</param>
		/// <returns>The logs (silently fails if the page is empty).</returns>
		public IEnumerable<LogEntry> GetPagedLogs(int pageIndex, int pageSize)
		{
			return GetPagedLogs(pageIndex, pageSize, LogLevel.Min);
		}

		/// <summary>Lazily loads a page of logs.</summary>
		/// <param name="pageIndex">The zero-based index of the page.</param>
		/// <param name="pageSize">The size of the page.</param>
		/// <param name="levelThreshold">Minimal log level (inclusive) for entries to be included.</param>
		/// <returns>The logs (silently fails if the page is empty).</returns>
		public IEnumerable<LogEntry> GetPagedLogs(int pageIndex, int pageSize, LogLevel levelThreshold)
		{
			Enforce.Argument(() => pageIndex, Rules.Is.AtLeast(0));
			Enforce.Argument(() => pageSize, Rules.Is.AtLeast(2), Rules.Is.AtMost(100));

			int skipItems = pageIndex * pageSize;

			int count = 0;
			foreach (var blobName in _provider.List(ContainerName, String.Empty))
			{
				if (count >= skipItems)
				{
					if (count - skipItems >= pageSize)
					{
						yield break;
					}

					var content = _provider.GetBlob<string>(ContainerName, blobName);
					if (!content.HasValue)
					{
						continue;
					}

					var entry = DecodeLogEntry(blobName, content.Value);
					if(EnumUtil.Parse<LogLevel>(entry.Level) < levelThreshold)
					{
						continue;
					}

					yield return entry;
				}
				count++;
			}
		}

		/// <summary>Deletes all the logs older than <paramref name="maxWeeks"/> weeks.</summary>
		/// <param name="maxWeeks">The max number of weeks of logs to preserve.</param>
		/// <remarks>The implementation is far from being efficient, but it is expected to be used sparingly.</remarks>
		public void DeleteOldLogs(int maxWeeks)
		{
			Enforce.Argument(() => maxWeeks, Rules.Is.AtLeast(1));

			DeleteOldLogs(DateTime.UtcNow.AddDays(-7 * maxWeeks));
		}

		// This is used for testing only
		// limit should be universal time
		internal void DeleteOldLogs(DateTime limit)
		{
			// Algorithm:
			// Iterate over the logs, queuing deletions up to 50 items at a time,
			// then restart; continue until no deletions are queued

			var deleteQueue = new List<string>(DeleteBatchSize);

			do
			{
				deleteQueue.Clear();

				foreach(var blobName in _provider.List(ContainerName, string.Empty))
				{
					var prefix = GetNamePrefix(blobName);
					var dateTime = ToDateTime(prefix);
					if(dateTime < limit) deleteQueue.Add(blobName);

					if(deleteQueue.Count == DeleteBatchSize) break;
				}

				foreach(var blobName in deleteQueue)
				{
					_provider.DeleteBlob(ContainerName, blobName);
				}
			} while(deleteQueue.Count > 0);
		}

		static string GetNewLogBlobName(LogLevel level)
		{
			var builder = new StringBuilder();
			builder.Append(ToPrefix(DateTime.UtcNow));
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

	///<summary>
	/// Log provider for the cloud logger
	///</summary>
	public class CloudLogProvider : ILogProvider
	{
		readonly IBlobStorageProvider _provider;

		public CloudLogProvider(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		ILog IProvider<string, ILog>.Get(string key)
		{
			return new CloudLogger(_provider, key);
		}
	}
}