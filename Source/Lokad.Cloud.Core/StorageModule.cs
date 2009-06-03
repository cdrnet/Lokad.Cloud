#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac.Builder;
using Microsoft.Samples.ServiceHosting.StorageClient;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Core
{
	public sealed class StorageModule : Module
	{
		public string AccountName { get; set; }
		public string AccountKey { get; set; }
		public string BlobEndpoint { get; set; }
		public string QueueEndpoint { get; set; }

		protected override void Load(ContainerBuilder builder)
		{
			if (RoleManager.IsRoleManagerRunning)
				ApplyOverridesFromRuntime();


			if (!string.IsNullOrEmpty(QueueEndpoint))
			{
				var queueUri = new Uri(QueueEndpoint);
				var accountInfo = new StorageAccountInfo(queueUri, null, AccountName, AccountKey);

				builder.Register(c =>
				{
					var queueService = QueueStorage.Create(accountInfo);

					ActionPolicy policy;
					if (c.TryResolve(out policy))
					{
						queueService.RetryPolicy = policy.Do;
					}

					return queueService;
				});
			}

			if (!string.IsNullOrEmpty(BlobEndpoint))
			{
				var blobUri = new Uri(BlobEndpoint);
				var accountInfo = new StorageAccountInfo(blobUri, null, AccountName, AccountKey);

				builder.Register(c =>
				{
					var storage = BlobStorage.Create(accountInfo);
					var policy = c.Resolve<ActionPolicy>();
					storage.RetryPolicy = policy.Do;
					return storage;
				});
			}
		}


		void ApplyOverridesFromRuntime()
		{
			//// get overrides from the role manager's settings
			//var settings = RoleManagerOpened
			//    .GetSettings()
			//    .ToDictionary(s => s.Key, s => s.Value);

			var properties = typeof(StorageModule).GetProperties();

			foreach (var info in properties)
			{
				var value = RoleManager.GetConfigurationSetting(info.Name);
				if (!string.IsNullOrEmpty(value))
				{
					info.SetValue(this, value, null);
				}
			}
		}
	}
}
