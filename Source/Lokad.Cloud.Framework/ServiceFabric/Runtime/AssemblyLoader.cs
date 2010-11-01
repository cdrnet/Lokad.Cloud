﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.ServiceFabric.Runtime
{
	/// <remarks>
	/// Since the assemblies are loaded in the current <c>AppDomain</c>, this class
	/// should be a natural candidate for a singleton design pattern. Yet, keeping
	/// it as a plain class facilitates the IoC instantiation.
	/// </remarks>
	public class AssemblyLoader
	{
		/// <summary>Name of the container used to store the assembly package.</summary>
		public const string ContainerName = "lokad-cloud-assemblies";

		/// <summary>Name of the blob used to store the assembly package.</summary>
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

		DateTimeOffset _lastPackageCheck;

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
			_lastPackageCheck = DateTimeOffset.UtcNow;

			// if no assemblies have been loaded yet, just skip the loading
			if (!buffer.HasValue)
			{
				return;
			}

			var resolver = new AssemblyResolver();
			resolver.Attach();

			var assemblies = new List<Pair<string, byte[]>>();
			var symbols = new Dictionary<string, byte[]>();

			using(var zipStream = new ZipInputStream(new MemoryStream(buffer.Value)))
			{
				ZipEntry entry;
				while((entry = zipStream.GetNextEntry()) != null)
				{
					if (!entry.IsFile)
					{
						continue;
					}

					var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
					var name = Path.GetFileNameWithoutExtension(entry.Name).ToLowerInvariant();
					if (extension != ".dll" && extension != ".pdb")
					{
						// skipping everything but assemblies and symbols
						continue;
					}

					var data = new byte[entry.Size];
					zipStream.Read(data, 0, data.Length);

					if (extension == ".dll")
					{
						assemblies.Add(Tuple.From(name, data));
					}
					else if(extension == ".pdb")
					{
						symbols.Add(name, data);
					}
				}
			}

			// loading assembly from data packed in zip
			foreach(var assembly in assemblies)
			{
				byte[] symbolBytes;
				if (symbols.TryGetValue(assembly.Key, out symbolBytes))
				{
					Assembly.Load(assembly.Value, symbolBytes);
				}
				else
				{
					Assembly.Load(assembly.Value);
				}
			}
		}

		public Maybe<byte[]> LoadConfiguration()
		{
			return _provider.GetBlob<byte[]>(ContainerName, ConfigurationBlobName, out _lastConfigurationEtag);
		}

		/// <summary>
		/// Reset the update status to the currently available version,
		/// such that <see cref="CheckUpdate"/> does not cause an update to happen.
		/// </summary>
		public void ResetUpdateStatus()
		{
			_lastPackageEtag = _provider.GetBlobEtag(ContainerName, PackageBlobName);
			_lastConfigurationEtag = _provider.GetBlobEtag(ContainerName, ConfigurationBlobName);
			_lastPackageCheck = DateTimeOffset.UtcNow;
		}

		/// <summary>Check for the availability of a new assembly package
		/// and throw a <see cref="TriggerRestartException"/> if a new package
		/// is available.</summary>
		/// <param name="delayCheck">If <c>true</c> then the actual update
		/// check if performed not more than the frequency specified by 
		/// <see cref="UpdateCheckFrequency"/>.</param>
		public void CheckUpdate(bool delayCheck)
		{
			var now = DateTimeOffset.UtcNow;

			// limiting the frequency where the actual update check is performed.
			if (delayCheck && now.Subtract(_lastPackageCheck) <= UpdateCheckFrequency)
			{
				return;
			}

			var newPackageEtag = _provider.GetBlobEtag(ContainerName, PackageBlobName);
			var newConfigurationEtag = _provider.GetBlobEtag(ContainerName, ConfigurationBlobName);

			if (!string.Equals(_lastPackageEtag, newPackageEtag))
			{
				throw new TriggerRestartException("Assemblies update has been detected.");
			}

			if (!string.Equals(_lastConfigurationEtag, newConfigurationEtag))
			{
				throw new TriggerRestartException("Configuration update has been detected.");
			}
		}
	}

	/// <summary>Resolves assemblies by caching assemblies that were loaded.</summary>
	public sealed class AssemblyResolver
	{
		/// <summary>
		/// Holds the loaded assemblies.
		/// </summary>
		private readonly Dictionary<string, Assembly> _assemblyCache;

		/// <summary> 
		/// Initializes an instance of the <see cref="AssemblyResolver" />  class.
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
			var isFullName = args.Name.IndexOf("Version=") != -1;

			// extract the simple name out of a qualified assembly name
			var nameOf = new Func<string, string>(qn => qn.Substring(0, qn.IndexOf(",")));

			// first try to find an already loaded assembly
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				if (isFullName)
				{
					if (assembly.FullName == args.Name ||
						nameOf(assembly.FullName) == nameOf(args.Name))
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

			// TODO: missing optimistic assembly resolution when it comes from the cache.

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