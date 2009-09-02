#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Framework;

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
		public const string BlobName = "default";

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

		DateTime _lastPackageCheck;

		/// <summary>Build a new package loader.</summary>
		public AssemblyLoader(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		/// <summary>Loads the assembly package.</summary>
		/// <remarks>This method is expected to be called only once. Call <see cref="CheckUpdate"/>
		/// afterward.</remarks>
		public void Load()
		{
			var buffer = _provider.GetBlob<byte[]>(ContainerName, BlobName, out _lastPackageEtag);
			_lastPackageCheck = DateTime.Now;

			// if no assemblies have been loaded yet, just skip the loading
			if(null == buffer) return;
			
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

		/// <summary>Check for the availability of a new assembly package
		/// and throw a <see cref="TriggerRestartException"/> if a new package
		/// is available.</summary>
		/// <param name="delayCheck">If <c>true</c> then the actual update
		/// check if performed not more than the frequency specified by 
		/// <see cref="UpdateCheckFrequency"/>.</param>
		public void CheckUpdate(bool delayCheck)
		{
			var now = DateTime.Now;

			// limiting the frequency where the actual update check is performed.
			if(now.Subtract(_lastPackageCheck) > UpdateCheckFrequency || !delayCheck)
			{
				var newEtag = _provider.GetBlobEtag(ContainerName, BlobName);

				if(!string.Equals(_lastPackageEtag, newEtag))
				{
					throw new TriggerRestartException("Assemblies update has been detected.");
				}
			}
		}
	}
}
