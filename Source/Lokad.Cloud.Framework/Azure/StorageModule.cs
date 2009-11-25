#region (c)2009 Lokad - New BSD license

// Copyright (c) Lokad 2009 
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence

#endregion

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Autofac.Builder;
using Lokad.Quality;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using Module=Autofac.Builder.Module;
using System;

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

		protected override void Load(ContainerBuilder builder)
		{
			if (RoleEnvironment.IsAvailable)
			{
				ApplyOverridesFromRuntime();
			}
			else
			{
				ApplyOverridesFromInternal();
			}

			/*if (!string.IsNullOrEmpty(DiagnosticsConnectionString))
			{
				var account = CloudStorageAccount.Parse(DiagnosticsConnectionString);
				var config = DiagnosticMonitor.GetDefaultInitialConfiguration();

				builder.Register(c =>
					{
						var monitor = DiagnosticMonitor.Start(account, config);

						return monitor;
					});
			}*/

			if (!string.IsNullOrEmpty(DataConnectionString))
			{
				CloudStorageAccount account = null;
				if(CloudStorageAccount.TryParse(DataConnectionString, out account))
				{
					// Only register storage components if the storage credentials are OK
					// This will cause exceptions to be thrown quite soon, but this way
					// the roles' OnStart() method returns correctly, allowing the web role
					// to display a warning to the user (the worker is recycled indefinitely
					// as Run() throws almost immediately)

					builder.Register(c =>
					{
						var queueService = account.CreateCloudQueueClient();
						queueService.RetryPolicy = BuildDefaultRetry();
						return queueService;
					});

					builder.Register(c =>
					{
						var storage = account.CreateCloudBlobClient();
						storage.RetryPolicy = BuildDefaultRetry();
						return storage;
					});


					// registering the Lokad.Cloud providers
					builder.Register(c =>
						{
							IFormatter formatter;
							if(!c.TryResolve(out formatter))
							{
								formatter = new BinaryFormatter();
							}

							return (IBlobStorageProvider)new BlobStorageProvider(c.Resolve<CloudBlobClient>(), formatter);
						});

					builder.Register(c =>
						{
							IFormatter formatter;
							if(!c.TryResolve(out formatter))
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
		}

		static RetryPolicy BuildDefaultRetry()
		{
			// [abdullin]: in short this gives us MinBackOff + 2^(10)*Rand.(~0.5.Seconds())
			// at the last retry. Reflect the method for more details
			var deltaBackoff = 0.5.Seconds();
			return RetryPolicies.RetryExponential(10, deltaBackoff);
		}

		/// <summary>
		/// Gets this type's properties whose value can be loaded by the IoC container.
		/// </summary>
		/// <returns>The properties.</returns>
		public static PropertyInfo[] GetProperties()
		{
			var properties = typeof (StorageModule).GetProperties();

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
			var result = new Dictionary<string, string>(5);

			foreach (var info in GetProperties())
			{
				var value = RoleEnvironment.GetConfigurationSettingValue(info.Name);
				if (!string.IsNullOrEmpty(value))
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
			if (OverriddenProperties == null) return;

			foreach (var info in GetProperties())
			{
				string value;
				OverriddenProperties.TryGetValue(info.Name, out value);
				if (!string.IsNullOrEmpty(value))
				{
					info.SetValue(this, value, null);
				}
			}
		}
	}
}