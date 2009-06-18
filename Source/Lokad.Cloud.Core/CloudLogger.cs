#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Security;
using System.Text;
using Microsoft.Samples.ServiceHosting.StorageClient;

namespace Lokad.Cloud.Core
{
	/// <summary>Logger built on top of the Blob Storage.</summary>
	/// <remarks>
	/// Logs are formatted in XML with
	/// <code>
	/// &lt;log&gt;
	///   &lt;message&gt; {0} &lt;/message&gt;
	///   &lt;error&gt; {1} &lt;/error&gt;
	/// &lt;/log&gt;
	/// </code>
	/// </remarks>
	public class CloudLogger : ILog
	{
		public const string ContainerName = "lokad-cloud-logs";
		public const string Delimiter = "/";

		BlobStorageProvider _provider;
		LogLevel _logLevelThreshold;

		/// <summary>Minimal log level (inclusive), below this level,
		/// notifications are ignored.</summary>
		public LogLevel LogLevelThreshold
		{
			get { return _logLevelThreshold;  }
			set { _logLevelThreshold = value; }
		}

		public CloudLogger(BlobStorageProvider provider)
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

		static string GetNewLogBlobName(LogLevel level)
		{
			var builder = new StringBuilder();
			builder.Append(level.ToString());
			builder.Append(Delimiter);
			builder.Append(DateTime.Now.ToUniversalTime().ToString("yyyy/MM/dd/hh/mm/ss/fff"));
			builder.Append(Delimiter);

			return builder.ToString();
		}
	}
}
