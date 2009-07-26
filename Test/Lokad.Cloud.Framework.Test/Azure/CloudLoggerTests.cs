#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using Lokad.Cloud.Framework;
using NUnit.Framework;

namespace Lokad.Cloud.Azure.Test
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

			logger.Info(new TriggerRestartException("CloudLoggerTests.Log"), "Not a restart, just a test.");
		}

		[Test]
		public void GetLogs()
		{
			var logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();


			int counter = 0;

			foreach(var entry in logger.GetRecentLogs())
			{
				Assert.IsNotNull(entry.Level, "#A00");
				Assert.IsTrue(entry.Level.Length > 3, "#A01");
				counter++;
				if(counter == 10) break; // don't retrieve more than 10 logs
			}

			Assert.Greater(counter, 0, "#A02");
		}

		[Test]
		public void ToPrefixToDateTime()
		{
			var now = DateTime.Now;

			var rounded = new DateTime(
				now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, DateTimeKind.Utc);

			var prefix = CloudLogger.ToPrefix(rounded);
			var roundedBis = CloudLogger.ToDateTime(prefix);

			Assert.AreEqual(rounded, roundedBis, "#A00");
		}
	}
}
