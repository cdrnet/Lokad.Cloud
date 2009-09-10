#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;

// HACK: the current impl just loads all the assemblies in the current AppDomain.
// This approach is simple, yet, the only way to reload a new package consists
// in crashing the worker with an uncaught exception. Ideally, the assemblies
// would be loaded in a separate AppDomain in order to speed-up the deployement
// of a new app.

namespace Lokad.Cloud.Azure
{
	/// <remarks>Since the assemblies are loaded in the current <c>AppDomain</c>, this
	/// class should be a natural candidate for a singleton design pattern. Yet, keeping
	/// it as a plain class facilitates the IoC instantiation.</remarks>
	public class AssemblyLoader
	{
		/// <summary>Name of the container used to store the assembly package.</summary>
		public const string ContainerName = "lokad-cloud-assemblies";

		/// <summary>Name of the blob used to store the asssembly package.</summary>
		public const string PackageBlobName = "default";

		/// <summary>Name of the blob used to store the client configuration.</summary>
		public const string ConfigurationBlobName = "config";

		/// <summary>Frequency for checking for update concerning the assembly package.</summary>
		public static TimeSpan UpdateCheckFrequency
		{
			get { return 1.Minutes(); }
		}

		readonly IBlobStorageProvider _provider;

		/// <summary>Etag of the assembly package. This property is set when
		/// assemblies are loaded. It can be used to monitor the availability of
		/// a new package.</summary>
		string _lastPackageEtag;

		string _lastConfigurationEtag;

		DateTime _lastPackageCheck;

		/// <summary>Build a new package loader.</summary>
		public AssemblyLoader(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		/// <summary>Loads the assembly package.</summary>
		/// <remarks>This method is expected to be called only once. Call <see cref="CheckUpdate"/>
		/// afterward.</remarks>
		public void LoadPackage()
		{
			var buffer = _provider.GetBlob<byte[]>(ContainerName, PackageBlobName, out _lastPackageEtag);
			_lastPackageCheck = DateTime.UtcNow;

			// if no assemblies have been loaded yet, just skip the loading
			if (null == buffer) return;

			var resolver = new AssemblyResolver();
			resolver.Attach();

			using(var zipStream = new ZipInputStream(new MemoryStream(buffer)))
			{
				ZipEntry entry;
				while((entry = zipStream.GetNextEntry()) != null)
				{
					var data = new byte[entry.Size];
					zipStream.Read(data, 0, (int)entry.Size);

					// skipping everything but assemblies
					if (!entry.IsFile || !entry.Name.ToLowerInvariant().EndsWith(".dll")) continue;

					// loading assembly from data packed in zip
					Assembly.Load(data);
				}
			}
		}

		public byte[] LoadConfiguration()
		{
			return _provider.GetBlob<byte[]>(ContainerName, ConfigurationBlobName, out _lastConfigurationEtag);
		}

		/// <summary>Check for the availability of a new assembly package
		/// and throw a <see cref="TriggerRestartException"/> if a new package
		/// is available.</summary>
		/// <param name="delayCheck">If <c>true</c> then the actual update
		/// check if performed not more than the frequency specified by 
		/// <see cref="UpdateCheckFrequency"/>.</param>
		public void CheckUpdate(bool delayCheck)
		{
			var now = DateTime.UtcNow;

			// limiting the frequency where the actual update check is performed.
			if(now.Subtract(_lastPackageCheck) > UpdateCheckFrequency || !delayCheck)
			{
				var newPackageEtag = _provider.GetBlobEtag(ContainerName, PackageBlobName);
				var newConfigurationEtag = _provider.GetBlobEtag(ContainerName, ConfigurationBlobName);

				if(!string.Equals(_lastPackageEtag, newPackageEtag))
				{
					throw new TriggerRestartException("Assemblies update has been detected.");
				}

				if (!string.Equals(_lastConfigurationEtag, newConfigurationEtag))
				{
					throw new TriggerRestartException("Configuration update has been detected.");
				}
			}
		}
	}

	/// <summary>Resolves assemblies by caching assemblies that were loaded.</summary>
	[Serializable]
	public sealed class AssemblyResolver
	{
		/// <summary>
		/// Holds the loaded assemblies.
		/// </summary>
		private readonly Dictionary<string, Assembly> _assemblyCache;

		/// <summary> 
		/// Initializes an instanse of the <see cref="AssemblyResolver" />  class.
		/// </summary>
		public AssemblyResolver()
		{
			_assemblyCache = new Dictionary<string, Assembly>();
		}

		/// <summary> 
		/// Installs the assembly resolver by hooking up to the 
		/// <see cref="AppDomain.AssemblyResolve" /> event.
		/// </summary>
		public void Attach()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoad;
		}

		/// <summary> 
		/// Uninstalls the assembly resolver.
		/// </summary>
		public void Detach()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
			AppDomain.CurrentDomain.AssemblyLoad -= AssemblyLoad;

			_assemblyCache.Clear();
		}


		/// <summary> 
		/// Resolves an assembly not found by the system using the assembly cache.
		/// </summary>
		private Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			bool isFullName = args.Name.IndexOf("Version=") != -1;

			// first try to find an already loaded assembly
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				if (isFullName)
				{
					if (assembly.FullName == args.Name)
					{
						// return assembly from AppDomain
						return assembly;
					}
				}
				else if (assembly.GetName(false).Name == args.Name)
				{
					// return assembly from AppDomain
					return assembly;
				}
			}

			// find assembly in cache
			if (isFullName)
			{
				if (_assemblyCache.ContainsKey(args.Name))
				{
					// return assembly from cache
					return _assemblyCache[args.Name];
				}
			}
			else
			{
				foreach (var assembly in _assemblyCache.Values)
				{
					if (assembly.GetName(false).Name == args.Name)
					{
						// return assembly from cache
						return assembly;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Occurs when an assembly is loaded. The loaded assembly is added 
		/// to the assembly cache.
		/// </summary>
		private void AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			// store assembly in cache
			_assemblyCache[args.LoadedAssembly.FullName] = args.LoadedAssembly;
		}
	}

}
