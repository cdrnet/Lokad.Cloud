#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion
using System;
using System.Linq;
using Lokad.Cloud;
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
		public void GetRecentLogs()
		{
			var logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

			logger.Error(
				new InvalidOperationException("CloudLoggerTests.Log"),
				"My message with CloudLoggerTests.Log.");

			logger.Info(new TriggerRestartException("CloudLoggerTests.Log"), "Not a restart, just a test.");

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
		public void GetPagedLogs()
		{
			var logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

			// Add 30 log messages
			for(int i = 0; i < 10; i++)
			{
				logger.Error(
					new InvalidOperationException("CloudLoggerTests.Log"),
					"My message with CloudLoggerTests.Log.");
				logger.Warn("A test warning");
				logger.Info(new TriggerRestartException("CloudLoggerTests.Log"), "Not a restart, just a test.");
			}

			Assert.AreEqual(10, logger.GetPagedLogs(0, 10).Count());
			Assert.AreEqual(10, logger.GetPagedLogs(1, 10).Count());
			Assert.AreEqual(10, logger.GetPagedLogs(2, 10).Count());
			Assert.IsTrue(logger.GetPagedLogs(1, 22).Count() >= 8);
			Assert.IsTrue(logger.GetPagedLogs(1, 25).Count() >= 5);
			Assert.AreEqual(0, logger.GetPagedLogs(100000, 20).Count());
		}

		[Test]
		public void DeleteOldLogs()
		{
			var logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

			int initialCount = logger.GetRecentLogs().Count();

			DateTime begin = DateTime.Now;
			for(int i = 0; i < 10; i++)
			{
				logger.Info("Just a test message");
			}

			Assert.AreEqual(initialCount + 10, logger.GetRecentLogs().Count());

			logger.DeleteOldLogs(begin.ToUniversalTime());

			Assert.AreEqual(10, logger.GetRecentLogs().Count());
		}

		[Test]
		public void ToPrefixToDateTime()
		{
			var now = DateTime.UtcNow;

			var rounded = new DateTime(
				now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond, DateTimeKind.Utc);

			var prefix = CloudLogger.ToPrefix(rounded);
			var roundedBis = CloudLogger.ToDateTime(prefix);

			Assert.AreEqual(rounded, roundedBis, "#A00");
		}
	}
}
