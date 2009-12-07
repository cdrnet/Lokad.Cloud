#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Lokad.Cloud.Azure;
using Lokad.Cloud.Diagnostics;
using System.IO;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace Lokad.Cloud.Web
{
	public static class GlobalSetup
	{
		public static readonly IContainer Container;

		static GlobalSetup()
		{
			var builder = new ContainerBuilder();

			// loading configuration from the Azure Service Configuration
			if (RoleEnvironment.IsAvailable)
			{
				builder.RegisterModule(new StorageModule());
			}
			else // or from the web.config directly (when azure config is not available)
			{
				builder.RegisterModule(new ConfigurationSettingsReader("autofac"));
			}

			builder.Register(c => new CloudLogger(c.Resolve<IBlobStorageProvider>())).As<ILog>();
			builder.RegisterModule(new DiagnosticsModule());

			Container = builder.Build();
		}

		#region Non-IoC members

		private static object _syncRoot = new object();
		private static string _storageAccountName = null;
		private static string _newLokadCloudVersion = "";
		private static bool? _lokadCloudUpToDate = null;
		private static readonly Regex VersionCheckRegex = new Regex(@"\<h2\>Download Version ([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\<\/h2\>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public const string DownloadUrl = "http://build.lokad.com/distrib/Lokad.Cloud/";

		/// <summary>Storage account name, cached at startup.</summary>
		public static string StorageAccountName
		{
			get
			{
				// This synchronization scheme is surely a bit overkill in this case...
				if(null == _storageAccountName)
				{
					lock(_syncRoot)
					{
						if(null == _storageAccountName)
							_storageAccountName = Container.Resolve<CloudBlobClient>().Credentials.AccountName;
					}
				}

				return _storageAccountName;
			}
		}

		/// <summary>Assembly version, cached on startup.</summary>
		public static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

		/// <summary>Info about Lokad.Cloud update status, cached at startup.</summary>
		public static bool? IsLokadCloudUpToDate
		{
			get
			{
				if(!_lokadCloudUpToDate.HasValue)
				{
					lock(_syncRoot)
					{
						if(!_lokadCloudUpToDate.HasValue)
						{
							var result = GetLokadCloudUpdateStatus();
							_newLokadCloudVersion = result.Item1 ? result.Item2 : null;
							_lokadCloudUpToDate = result.Item1 ? (bool?)(_newLokadCloudVersion == null) : null;
						}
					}
				}

				return _lokadCloudUpToDate;
			}
		}

		/// <summary>The new Lokad.Cloud version, if any.</summary>
		public static string NewLokadCloudVersion
		{
			get { return _newLokadCloudVersion; }
		}

		/// <summary>Retrieves the update status of Lokad.Cloud from the Internet.</summary>
		/// <returns>A value indicating whether the version check was completed and 
		/// the new version of Lokad.Cloud, or <c>null</c> if no new version is found.</returns>
		private static Tuple<bool, string> GetLokadCloudUpdateStatus()
		{
			// HACK: Temporary implementation that looks for version number strings in http://build.lokad.com/distrib/Lokad.Cloud/

			try
			{
				var request = (HttpWebRequest)HttpWebRequest.Create(DownloadUrl);

				var response = (HttpWebResponse)request.GetResponse();
				if(response.StatusCode != HttpStatusCode.OK) return new Tuple<bool, string>(false, null);

				string responseContent = null;
				using(var reader = new StreamReader(response.GetResponseStream()))
				{
					responseContent = reader.ReadToEnd();
				}

				var match = VersionCheckRegex.Match(responseContent);
				if(!match.Success) return new Tuple<bool, string>(false, null);

				var latestVersion = match.Groups[1].Value;

				if(latestVersion == AssemblyVersion) return new Tuple<bool, string>(true, null);
				else return new Tuple<bool, string>(true, latestVersion);
			}
			catch
			{
				return new Tuple<bool, string>(false, null);
			}
		}

		#endregion

	}
}