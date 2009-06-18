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
	public class CloudLoggerTests
	{
		[Test]
		public void Log()
		{
			var logger = GlobalSetup.Container.Resolve<ILog>();

			logger.Error(
				new InvalidOperationException("CloudLoggerTests.Log"), 
				"My message with CloudLoggerTests.Log.");
		}
	}
}
