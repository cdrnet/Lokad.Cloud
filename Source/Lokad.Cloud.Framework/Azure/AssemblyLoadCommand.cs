#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using Lokad.Cloud.Core;

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
	public class AssemblyLoadCommand : ICommand
	{
		static string _lastPackageEtag;

		/// <summary>Name of the container used to store the assembly package.</summary>
		public const string ContainerName = "lokad-cloud-assemblies";

		/// <summary>Name of the blob used to store the asssembly package.</summary>
		public const string BlobName = "default";

		readonly IBlobStorageProvider _provider;

		/// <summary>Etag of the assembly package. This property is set when
		/// assemblies are loaded. It can be used to monitor the availability of
		/// a new package.</summary>
		public static string LastPackageEtag
		{
			get { return _lastPackageEtag; }
		}

		public AssemblyLoadCommand(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		public void Execute()
		{
			var buffer = _provider.GetBlob<byte[]>(ContainerName, BlobName, out _lastPackageEtag);

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
	}
}
