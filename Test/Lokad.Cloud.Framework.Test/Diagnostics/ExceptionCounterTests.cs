#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Linq;
using Lokad.Cloud.Diagnostics;
using NUnit.Framework;
using Lokad.Diagnostics;

namespace Lokad.Cloud.Azure.Test
{
	[TestFixture]
	public class ExceptionCounterTests
	{
		[Test]
		public void Tracked_exceptions_make_it_to_the_statistics()
		{
			var monitor = new ExceptionTrackingMonitor(GlobalSetup.Container.Resolve<IBlobStorageProvider>());

			long count = monitor.GetStatistics()
				.SelectMany(s => s.Statistics)
				.Where(e => e.Text.Contains("Test Exception"))
				.Sum(e => e.Count);

			ExceptionCounters.Default.Add(new Exception("Test Exception"));
			monitor.UpdateStatistics();

			Assert.AreEqual(
				count + 1,
				monitor.GetStatistics()
					.SelectMany(s => s.Statistics)
					.Where(e => e.Text.Contains("Test Exception"))
					.Sum(e => e.Count));
		}
	}
}
