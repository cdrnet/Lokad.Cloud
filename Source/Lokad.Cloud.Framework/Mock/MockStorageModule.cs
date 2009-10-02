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
using Microsoft.Samples.ServiceHosting.StorageClient;
using Microsoft.ServiceHosting.ServiceRuntime;
using Lokad.Cloud.Mock;

namespace Lokad.Cloud.Mock
{
	[NoCodeCoverage]
	public sealed class MockStorageModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.Register(c =>
         	{
         		IFormatter formatter;
         		if (!c.TryResolve(out formatter))
         		{
         			formatter = new BinaryFormatter();
         		}

         		return (IBlobStorageProvider)new MemoryBlobStorageProvider();
         	});

			builder.Register(c =>
			{
				IFormatter formatter;
				if (!c.TryResolve(out formatter))
				{
					formatter = new BinaryFormatter();
				}

				return (IQueueStorageProvider)new MemoryQueueStorageProvider(formatter);
			});
		}

	}
}
