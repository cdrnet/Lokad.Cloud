#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Lokad.Cloud.Core.Test
{
	[TestFixture]
	public class ServiceBalancerCommandTests
	{
		[Test]
		public void Constructor()
		{
			// validating the service invokation through reflection
			var command = GlobalSetup.Container.Resolve<ServiceBalancerCommand>();

			Assert.Greater(command.Services.Length, 1, "#A00 System services should be loaded by default.");
		}
	}
}
