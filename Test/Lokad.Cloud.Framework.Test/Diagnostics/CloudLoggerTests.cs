#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.ServiceFabric.Runtime;
using Lokad.Cloud.Storage;
using Lokad.Cloud.Test;
using NUnit.Framework;

namespace Lokad.Cloud.Diagnostics.Test
{
	[TestFixture]
	public class CloudLoggerTests
	{
		readonly CloudLogger Logger = (CloudLogger)GlobalSetup.Container.Resolve<ILog>();

		[TestFixtureSetUp]
		public void Setup()
		{
			// HACK: deleting all logs to avoid serialization errors due to evolution
			var provider = GlobalSetup.Container.Resolve<IBlobStorageProvider>();
			foreach(var blobName in provider.List(CloudLogger.ContainerName, ""))
			{
				provider.DeleteBlob(CloudLogger.ContainerName, blobName);
			}
		}

		[Test]
		public void Log()
		{
			Logger.Error(
				new InvalidOperationException("CloudLoggerTests.Log"), 
				"My message with CloudLoggerTests.Log.");

			Logger.Info(new TriggerRestartException("CloudLoggerTests.Log"), "Not a restart, just a test.");
		}

		[Test]
		public void GetRecentLogs()
		{
			Logger.Error(
				new InvalidOperationException("CloudLoggerTests.Log"),
				"My message with CloudLoggerTests.Log.");

			Logger.Info(new TriggerRestartException("CloudLoggerTests.Log"), "Not a restart, just a test.");

			int counter = 0;

			foreach(var entry in Logger.GetRecentLogs())
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
			// Add 30 log messages
			for(int i = 0; i < 10; i++)
			{
				Logger.Error(
					new InvalidOperationException("CloudLoggerTests.Log"),
					"My message with CloudLoggerTests.Log.");
				Logger.Warn("A test warning");
				Logger.Info(new TriggerRestartException("CloudLoggerTests.Log"), "Not a restart, just a test.");
			}

			Assert.AreEqual(10, Logger.GetPagedLogs(0, 10).Count());
			Assert.AreEqual(10, Logger.GetPagedLogs(1, 10).Count());
			Assert.AreEqual(10, Logger.GetPagedLogs(2, 10).Count());
			Assert.IsTrue(Logger.GetPagedLogs(1, 22).Count() >= 8);
			Assert.IsTrue(Logger.GetPagedLogs(1, 25).Count() >= 5);
			Assert.AreEqual(0, Logger.GetPagedLogs(100000, 20).Count());
		}

		[Test]
		public void DeleteOldLogs()
		{
			int initialCount = Logger.GetRecentLogs().Count();

			var begin = DateTime.Now;
			System.Threading.Thread.Sleep(1000); // Wait to make sure that logs are created after 'begin'

			for(int i = 0; i < 10; i++)
			{
				Logger.Info("Just a test message");
			}

			Assert.AreEqual(initialCount + 10, Logger.GetRecentLogs().Count());

			Logger.DeleteOldLogs(begin.ToUniversalTime());

			Assert.AreEqual(10, Logger.GetRecentLogs().Count());
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
