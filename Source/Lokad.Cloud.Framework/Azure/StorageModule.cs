#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Autofac.Builder;
using Lokad.Quality;
using Microsoft.Samples.ServiceHosting.StorageClient;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Azure
{
	/// <summary>IoC module that auto-load <see cref="StorageAccountInfo"/>, 
	/// <see cref="BlobStorage"/> and <see cref="QueueStorage"/> from the 
	/// properties.</summary>
	[NoCodeCoverage]
	public sealed class StorageModule : Module
	{
		/// <summary>Account name of the Azure Storage.</summary>
		[UsedImplicitly]
		public string AccountName { get; set; }

		/// <summary>Key to access the Azure Storage.</summary>
		[UsedImplicitly]
		public string AccountKey { get; set; }

		/// <summary>Indicates whether the account key is encrypted with DBAPI.</summary>
		[UsedImplicitly]
		public string IsStorageKeyEncrypted { get; set; }

		/// <summary>URL of the Blob Storage.</summary>
		[UsedImplicitly]
		public string BlobEndpoint { get; set; }

		/// <summary>URL of the Queue Storage.</summary>
		[UsedImplicitly]
		public string QueueEndpoint { get; set; }

		/// <summary>Provides configuration properties when they are not available from RoleManager.</summary>
		public Dictionary<string, string> OverriddenProperties { get; set; }

		protected override void Load(ContainerBuilder builder)
		{
			if(RoleManager.IsRoleManagerRunning)
			{
				ApplyOverridesFromRuntime();
			}
			else
			{
				ApplyOverridesFromInternal();
			}

			if (!string.IsNullOrEmpty(QueueEndpoint))
			{
				var queueUri = new Uri(QueueEndpoint);
				var accountInfo = new StorageAccountInfo(queueUri, null, AccountName, GetAccountKey());

				builder.Register(c =>
				{
					var queueService = QueueStorage.Create(accountInfo);

					ActionPolicy policy;
					if (!c.TryResolve(out policy))
					{
						policy = DefaultPolicy();	
					}

					queueService.RetryPolicy = policy.Do;

					return queueService;
				});
			}

			if (!string.IsNullOrEmpty(BlobEndpoint))
			{
				var blobUri = new Uri(BlobEndpoint);
				var accountInfo = new StorageAccountInfo(blobUri, null, AccountName, GetAccountKey());

				builder.Register(c =>
				{
					var storage = BlobStorage.Create(accountInfo);
					
					ActionPolicy policy;
					if (!c.TryResolve(out policy))
					{
						policy = DefaultPolicy();
					}

					storage.RetryPolicy = policy.Do;
					return storage;
				});
			}

			// registering the Lokad.Cloud providers
			if (!string.IsNullOrEmpty(QueueEndpoint) && !string.IsNullOrEmpty(BlobEndpoint))
			{
				builder.Register(c =>
             	{
             		IFormatter formatter;
             		if (!c.TryResolve(out formatter))
             		{
             			formatter = new BinaryFormatter();
             		}

             		return (IBlobStorageProvider)new BlobStorageProvider(c.Resolve<BlobStorage>(), formatter);
             	});

				builder.Register(c =>
				{
					IFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new BinaryFormatter();
					}

					return (IQueueStorageProvider)new QueueStorageProvider(
						c.Resolve<QueueStorage>(),
						c.Resolve<BlobStorage>(),
						formatter);
				});
			}
		}

		static ActionPolicy DefaultPolicy()
		{
			return ActionPolicy
				.With(HandleException)
				.Retry(10, (e, i) => SystemUtil.Sleep(5.Seconds()));
		}

		static bool HandleException(Exception ex)
		{
			return ex is StorageServerException;
		}

		string GetAccountKey()
		{
			return "true".Equals((IsStorageKeyEncrypted ?? string.Empty).ToLower()) ? 
				DBAPI.Decrypt(AccountKey) : AccountKey;
		}

		/// <summary>
		/// Gets this type's properties whose value can be loaded by the IoC container.
		/// </summary>
		/// <returns>The properties.</returns>
		public static System.Reflection.PropertyInfo[] GetProperties()
		{
			var properties = typeof(StorageModule).GetProperties();

			return
				(from p in properties
				 where p.Name != "IsStorageKeyEncrypted" && p.Name != "OverriddenProperties"
				 select p).ToArray();
		}

		/// <summary>
		/// Gets the properties values from the Azure runtime.
		/// </summary>
		/// <returns>The properties values.</returns>
		public static Dictionary<string, string> GetPropertiesValuesFromRuntime()
		{
			Dictionary<string, string> result = new Dictionary<string, string>(5);

			foreach(var info in GetProperties())
			{
				var value = RoleManager.GetConfigurationSetting(info.Name);
				if(!string.IsNullOrEmpty(value))
				{
					result.Add(info.Name, value);
				}
			}

			return result;
		}

		void ApplyOverridesFromRuntime()
		{
			Dictionary<string, string> values = GetPropertiesValuesFromRuntime();

			foreach (var info in GetProperties())
			{
				string value = null;
				values.TryGetValue(info.Name, out value);
				if (!string.IsNullOrEmpty(value))
				{
					info.SetValue(this, value, null);
				}
			}
		}

		void ApplyOverridesFromInternal()
		{
			if(OverriddenProperties == null) return;

			foreach(var info in GetProperties())
			{
				string value = null;
				OverriddenProperties.TryGetValue(info.Name, out value);
				if(!string.IsNullOrEmpty(value))
				{
					info.SetValue(this, value, null);
				}
			}
		}

	}
}
