#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Lokad.Cloud.Core.Test
{
	[TestFixture]
	public class AssemblyLoadCommandTests
	{
		[Test]
		public void Execute()
		{
			byte[] buffer;
			using (var dllFile = new FileStream("../../Sample/sample.dll.zip", FileMode.Open))
			{
				buffer = new byte[dllFile.Length];
				dllFile.Read(buffer, 0, buffer.Length);
			}

			IBlobStorageProvider provider = GlobalSetup.Container.Resolve<BlobStorageProvider>();
			provider.CreateContainer(AssemblyLoadCommand.DefaultContainerName);

			// put the sample assembly
			provider.PutBlob(
				AssemblyLoadCommand.DefaultContainerName, AssemblyLoadCommand.DefaultBlobName, buffer);

			var command = GlobalSetup.Container.Resolve<AssemblyLoadCommand>();

			command.Execute();

			// validate that 'sample.dll' has been loaded
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assert.That(assemblies.Any(a => a.ManifestModule.ScopeName == "sample.dll"));
		}
	}
}
