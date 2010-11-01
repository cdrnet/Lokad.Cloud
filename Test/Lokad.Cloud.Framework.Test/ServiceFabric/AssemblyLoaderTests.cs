﻿#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.IO;
using System.Linq;
using Lokad.Cloud.Management;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Test;
using Lokad.Diagnostics;
using NUnit.Framework;

namespace Lokad.Cloud.ServiceFabric.Test
{
	[TestFixture]
	public class AssemblyLoaderTests
	{
		[Test]
		public void LoadCheck()
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
			provider.CreateContainer(AssemblyLoader.ContainerName);

			// put the sample assembly
			provider.PutBlob(AssemblyLoader.ContainerName, AssemblyLoader.PackageBlobName, buffer);

			var loader = new AssemblyLoader(provider);
			loader.LoadPackage();
			loader.LoadConfiguration();

			// validate that 'sample.dll' has been loaded
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assert.That(assemblies.Any(a => a.FullName.StartsWith("sample")));

			// validate using management class
			var cloudAssemblies = new CloudAssemblies(provider, NullLog.Instance);
			Assert.That(cloudAssemblies.GetAssemblies().Any(a => a.AssemblyName.StartsWith("sample")));

			// no update, checking
			try
			{
				loader.CheckUpdate(false);
			}
			catch (TriggerRestartException)
			{
				Assert.Fail("Package has not been updated yet.");
			}

			// forcing update, this time using the management class
			cloudAssemblies.UploadAssemblyZipContainer(buffer);

			// update, re-checking
			try
			{
				loader.CheckUpdate(false);
				Assert.Fail("Update should have been detected.");
			}
			catch (TriggerRestartException)
			{
				// do nothing
			}

		}
	}
}
