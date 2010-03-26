#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Threading;
using Autofac.Builder;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <summary>
	/// AppDomain-isolated host for a single runtime instance.
	/// </summary>
	internal class IsolatedSingleRuntimeHost
	{
		/// <summary>Refer to the callee instance (isolated). This property is not null
		/// only for the caller instance (non-isolated).</summary>
		volatile SingleRuntimeHost _isolatedInstance;

		/// <summary>
		/// Run the hosted runtime, blocking the calling thread.
		/// </summary>
		/// <returns>True if the worker stopped as planned (e.g. due to updated assemblies)</returns>
		public bool Run()
		{
			var settings = RoleConfigurationSettings.LoadFromRoleEnvironment();

			// The trick is to load this same assembly in another domain, then
			// instantiate this same class and invoke Run
			var domain = AppDomain.CreateDomain("WorkerDomain", null, AppDomain.CurrentDomain.SetupInformation);

			bool restartForAssemblyUpdate;

			try
			{
				_isolatedInstance = (SingleRuntimeHost)domain.CreateInstanceAndUnwrap(
					Assembly.GetExecutingAssembly().FullName,
					typeof(SingleRuntimeHost).FullName);

				// This never throws, unless something went wrong with IoC setup and that's fine
				// because it is not possible to execute the worker
				restartForAssemblyUpdate = _isolatedInstance.Run(settings);
			}
			finally
			{
				_isolatedInstance = null;

				// If this throws, it's because something went wrong when unloading the AppDomain
				// The exception correctly pulls down the entire worker process so that no AppDomains are
				// left in memory
				AppDomain.Unload(domain);
			}

			return restartForAssemblyUpdate;
		}

		/// <summary>
		/// Immediately stop the runtime host and wait until it has exited (or a timeout expired).
		/// </summary>
		public void Stop()
		{
			var instance = _isolatedInstance;
			if (null != instance)
			{
				_isolatedInstance.Stop();
			}
		}

		/// <summary>
		/// Interrupt the runtime host at the next point where it fits well,
		/// without forcibly aborting running jobs. Does not wait until it has exited.
		/// </summary>
		public void RequestToStop()
		{
			var instance = _isolatedInstance;
			if (null != instance)
			{
				_isolatedInstance.RequestToStop();
			}
		}
	}

	/// <summary>
	/// Host for a single runtime instance.
	/// </summary>
	internal class SingleRuntimeHost : MarshalByRefObject, IDisposable
	{
		/// <summary>Current hosted runtime instance.</summary>
		volatile Runtime _runtime;

		/// <summary>
		/// Manual-reset wait handle, signaled once the host stopped running.
		/// </summary>
		readonly EventWaitHandle _stoppedWaitHandle = new ManualResetEvent(false);

		/// <summary>
		/// Run the hosted runtime, blocking the calling thread.
		/// </summary>
		/// <returns>True if the worker stopped as planned (e.g. due to updated assemblies)</returns>
		public bool Run(Maybe<ICloudConfigurationSettings> externalRoleConfiguration)
		{
			_stoppedWaitHandle.Reset();

			// IoC Setup

			var builder = new ContainerBuilder();
			builder.RegisterModule(new CloudModule());
			if (externalRoleConfiguration.HasValue)
			{
				builder.RegisterModule(new CloudConfigurationModule(externalRoleConfiguration.Value));
			}
			else
			{
				builder.RegisterModule(new CloudConfigurationModule());
			}

			builder.Register(typeof (Runtime)).FactoryScoped();

			// Run

			using (var container = builder.Build())
			{
				var log = container.Resolve<ILog>();

				_runtime = null;
				try
				{
					_runtime = container.Resolve<Runtime>();
					_runtime.RuntimeContainer = container;

					// runtime endlessly keeps pinging queues for pending work
					_runtime.Execute();
					log.Log(LogLevel.Warn, "Runtime host stopped execution.");
				}
				catch (TypeLoadException typeLoadEx)
				{
					log.Log(LogLevel.Error, typeLoadEx, string.Format(
						"Type {0} could not be loaded (service: {1}).",
						typeLoadEx.TypeName,
						GetServiceInExecution(_runtime)));
				}
				catch (FileLoadException fileLoadEx)
				{
					// Tentatively: referenced assembly is missing
					log.Log(LogLevel.Error, fileLoadEx, string.Format(
						"Could not load assembly probably due to a missing reference assembly (service: {0}).",
						GetServiceInExecution(_runtime)));
				}
				catch (SecurityException securityEx)
				{
					// Tentatively: assembly cannot be loaded due to security config
					log.Log(LogLevel.Error, securityEx, string.Format(
						"Could not load assembly {0} probably due to security configuration (service: {1}).",
						securityEx.FailedAssemblyInfo,
						GetServiceInExecution(_runtime)));
				}
				catch (TriggerRestartException)
				{
					log.Log(LogLevel.Warn, "Runtime host was triggered to stop execution.");
					return true;
				}
				catch (ThreadInterruptedException)
				{
					log.Log(LogLevel.Warn, "Runtime host interrupted execution.");
				}
				catch (ThreadAbortException)
				{
					log.Log(LogLevel.Warn, "Runtime host aborted execution.");
					Thread.ResetAbort();
				}
				catch (Exception ex)
				{
					// Generic exception
					log.Log(LogLevel.Error, ex, string.Format(
						"An unhandled exception occurred (service: {0}).",
						GetServiceInExecution(_runtime)));
				}
				finally
				{
					_stoppedWaitHandle.Set();
					_runtime = null;
				}

				return false;
			}
		}

		/// <summary>
		/// Immediately stop the runtime host and wait until it has exited (or a timeout expired).
		/// </summary>
		public void Stop()
		{
			var runtime = _runtime;
			if (null != runtime)
			{
				runtime.Stop();

				// note: we DO have to wait until the shut down has finished,
				// or the Azure Fabric will tear us apart early!
				_stoppedWaitHandle.WaitOne(25.Seconds());
			}
		}

		/// <summary>
		/// Interrupt the runtime host at the next point where it fits well,
		/// without forcibly aborting running jobs. Does not wait until it has exited.
		/// </summary>
		public void RequestToStop()
		{
			var runtime = _runtime;
			if (null != runtime)
			{
				runtime.RequestToStop();
			}
		}

		static string GetServiceInExecution(Runtime runtime)
		{
			string service;
			return runtime == null || String.IsNullOrEmpty(service = runtime.ServiceInExecution)
				? "unknown"
				: service;
		}

		public void Dispose()
		{
			_stoppedWaitHandle.Close();
		}
	}
}
