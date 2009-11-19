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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Diagnostics;

namespace Lokad.Cloud.Azure
{
	/// <summary>IoC module that auto-load <see cref="StorageAccountInfo"/>, 
	/// <see cref="BlobStorage"/> and <see cref="QueueStorage"/> from the 
	/// properties.</summary>
	[NoCodeCoverage]
	public sealed class StorageModule : Module
	{
		/// <summary>The data connection string.</summary>
		[UsedImplicitly]
		public string DataConnectionString { get; set; }

		/// <summary>The diagnostics connection string.</summary>
		[UsedImplicitly]
		public string DiagnosticsConnectionString { get; set; }

		/// <summary>Provides configuration properties when they are not available from RoleManager.</summary>
		public Dictionary<string, string> OverriddenProperties { get; set; }

		static bool RetryPolicy(int retryCount, Exception lastException, out TimeSpan delay)
		{
			if(retryCount < 10 && lastException is StorageServerException)
			{
				delay = 5.Seconds();
				return true;
			}
			else
			{
				delay = TimeSpan.Zero;
				return false;
			}
		}

		protected override void Load(ContainerBuilder builder)
		{
			if(RoleEnvironment.IsAvailable)
			{
				ApplyOverridesFromRuntime();
			}
			else
			{
				ApplyOverridesFromInternal();
			}

			if(!string.IsNullOrEmpty(DiagnosticsConnectionString))
			{
				var account = CloudStorageAccount.Parse(DiagnosticsConnectionString);
				var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

				builder.Register(c =>
				{
					var monitor = DiagnosticMonitor.Start(account, config);

					return monitor;
				});
			}

			if (!string.IsNullOrEmpty(DataConnectionString))
			{
				var account = CloudStorageAccount.Parse(DataConnectionString);

				builder.Register(c =>
				{
					var queueService = account.CreateCloudQueueClient();

					// TODO: verify a better handling of retry policy

					/*ActionPolicy policy;
					if (!c.TryResolve(out policy))
					{
						policy = DefaultPolicy();	
					}*/
					
					//queueService.RetryPolicy = policy.Do;
					//queueService.RetryPolicy = () => RetryPolicy;

					return queueService;
				});
			}

			if (!string.IsNullOrEmpty(DataConnectionString))
			{
				var account = CloudStorageAccount.Parse(DataConnectionString);

				builder.Register(c =>
				{
					var storage = account.CreateCloudBlobClient();

					// TODO: verify a better handling of retry policy
					
					/*ActionPolicy policy;
					if (!c.TryResolve(out policy))
					{
						policy = DefaultPolicy();
					}*/

					//storage.RetryPolicy = policy.Do;
					//storage.RetryPolicy = () => RetryPolicy;

					return storage;
				});
			}

			// registering the Lokad.Cloud providers
			if (!string.IsNullOrEmpty(DataConnectionString))
			{
				builder.Register(c =>
             	{
             		IFormatter formatter;
             		if (!c.TryResolve(out formatter))
             		{
             			formatter = new BinaryFormatter();
             		}

             		return (IBlobStorageProvider)new BlobStorageProvider(c.Resolve<CloudBlobClient>(), formatter);
             	});

				builder.Register(c =>
				{
					IFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new BinaryFormatter();
					}

					return (IQueueStorageProvider)new QueueStorageProvider(
						c.Resolve<CloudQueueClient>(),
						c.Resolve<CloudBlobClient>(),
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

		/// <summary>
		/// Gets this type's properties whose value can be loaded by the IoC container.
		/// </summary>
		/// <returns>The properties.</returns>
		public static System.Reflection.PropertyInfo[] GetProperties()
		{
			var properties = typeof(StorageModule).GetProperties();

			return
				(from p in properties
				 where p.Name != "OverriddenProperties"
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
				var value = RoleEnvironment.GetConfigurationSettingValue(info.Name);
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
