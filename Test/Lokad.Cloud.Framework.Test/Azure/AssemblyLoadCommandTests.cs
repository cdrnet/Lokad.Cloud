﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using Lokad.Cloud.Core;
using NUnit.Framework;

namespace Lokad.Cloud.Azure.Test
{
	[TestFixture]
	public class AssemblyLoadCommandTests
	{
		[Test]
		public void Execute()
		{
			var path = @"..\..\Sample\sample.dll.zip";
			if(!File.Exists(path))
			{
				// special casing the integration server
				path = @"..\..\Test\Lokad.Cloud.Framework.Test\Sample\sample.dll.zip";
			}

			byte[] buffer;
			using (var dllFile = new FileStream(path, FileMode.Open))
			{
				buffer = new byte[dllFile.Length];
				dllFile.Read(buffer, 0, buffer.Length);
			}

			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
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
