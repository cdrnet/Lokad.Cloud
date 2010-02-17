#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Lokad.Cloud.Web
{
	public enum LokadCloudVersionState
	{
		Unknown,
		UpToDate,
		UpdateAvailable
	}

	public class LokadCloudVersion
	{
		const string _downloadUrl = "http://build.lokad.com/distrib/Lokad.Cloud/";
		const string _regexPattern = @"\<h2\>Download Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\<\/h2\>";

		readonly object _sync = new object();
		readonly Regex _regex = new Regex(_regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
		readonly TimeSpan _checkPeriod = 1.Days();

		Maybe<Version> _newestVersion;
		DateTimeOffset _lastChecked;

		public Version RunningVersion { get; private set; }

		public LokadCloudVersion()
		{
			RunningVersion = Assembly.GetExecutingAssembly().GetName().Version;
			_newestVersion = Maybe<Version>.Empty;
		}

		public Uri DownloadUri
		{
			get { return new Uri(_downloadUrl); }
		}

		public Maybe<Version> NewestVersion
		{
			get
			{
				var now = DateTimeOffset.UtcNow;

				// request new info if last update was a long time ago
				if (now.Subtract(_checkPeriod) > _lastChecked)
				{
					lock (_sync)
					{
						if (now.Subtract(_checkPeriod) > _lastChecked)
						{
							_lastChecked = now;
							var newestVersion = RequestNewestVersionInfo().ToMaybe(v => v);
							if(newestVersion.HasValue)
							{
								_newestVersion = newestVersion;
							}
						}
					}
				}

				// might still be empty if no request succeeded up to now
				return _newestVersion;
			}
		}

		public LokadCloudVersionState VersionState
		{
			get
			{
				var newestVersion = NewestVersion;
				if(!newestVersion.HasValue)
				{
					return LokadCloudVersionState.Unknown;
				}

				return newestVersion == RunningVersion
					? LokadCloudVersionState.UpToDate
					: LokadCloudVersionState.UpdateAvailable;
			}
		}

		public bool IsUpdateAvailable
		{
			get { return VersionState == LokadCloudVersionState.UpdateAvailable; }
		}

		public bool IsUpToDate
		{
			get { return VersionState == LokadCloudVersionState.UpToDate; }
		}

		Result<Version> RequestNewestVersionInfo()
		{
			// HACK: Temporary implementation that looks for version number strings in http://build.lokad.com/distrib/Lokad.Cloud/

			try
			{
				var request = WebRequest.Create(_downloadUrl);

				var response = (HttpWebResponse)request.GetResponse();
				if (response.StatusCode != HttpStatusCode.OK)
				{
					return Result<Version>.CreateError(response.StatusCode.ToString());
				}

				string responseContent;
				using (var reader = new StreamReader(response.GetResponseStream()))
				{
					responseContent = reader.ReadToEnd();
				}

				var match = _regex.Match(responseContent);
				if (!match.Success)
				{
					return Result<Version>.CreateError("UnexpectedResponseFormat");
				}

				return Result.CreateSuccess(new Version(match.Groups[1].Value));
			}
			catch(Exception e)
			{
				return Result<Version>.CreateError(e.Message);
			}
		}
	}
}
