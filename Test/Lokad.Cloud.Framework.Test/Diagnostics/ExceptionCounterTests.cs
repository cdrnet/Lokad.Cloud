#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Azure.Test;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using NUnit.Framework;
using Lokad.Diagnostics;

namespace Lokad.Cloud.Test.Diagnostics
{
	[TestFixture]
	public class ExceptionCounterTests
	{
		[Test]
		public void Tracked_exceptions_make_it_to_the_statistics()
		{
			var monitor = new ExceptionTrackingMonitor(GlobalSetup.Container.Resolve<ICloudDiagnosticsRepository>());
			var statistics = new CloudStatistics(GlobalSetup.Container.Resolve<ICloudDiagnosticsRepository>());

			var countBefore = statistics.GetAllTrackedExceptions()
				.SelectMany(s => s.Statistics)
				.Where(e => e.Text.Contains("Test Exception"))
				.Sum(e => e.Count);

			ExceptionCounters.Default.Add(new Exception("Test Exception"));
			monitor.UpdateStatistics();

			Assert.AreEqual(
				countBefore + 1,
				statistics.GetAllTrackedExceptions()
					.SelectMany(s => s.Statistics)
					.Where(e => e.Text.Contains("Test Exception"))
					.Sum(e => e.Count));
		}
	}
}