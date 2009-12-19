#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using Lokad.Cloud.Azure.Test;
using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using NUnit.Framework;
using Lokad.Diagnostics;

namespace Lokad.Cloud.Test.Diagnostics
{
	[TestFixture]
	public class ExecutionCounterTests
	{
		[Test]
		public void Execution_profiles_make_it_to_the_statistics()
		{
			var monitor = new ExecutionProfilingMonitor(GlobalSetup.Container.Resolve<ICloudDiagnosticsRepository>());
			var statistics = new CloudStatistics(GlobalSetup.Container.Resolve<ICloudDiagnosticsRepository>());

			var count = statistics.GetAllExecutionProfiles()
				.SelectMany(s => s.Statistics)
				.Where(e => e.Name.Contains("Test Profile"))
				.Sum(e => e.OpenCount);

			var counter = new ExecutionCounter("Test Profile", 0, 0);
			var timestamp = counter.Open();
			counter.Close(timestamp);

			ExecutionCounters.Default.RegisterRange(new[] {counter});
			monitor.UpdateStatistics();

			Assert.AreEqual(
				count + 1,
				statistics.GetAllExecutionProfiles()
					.SelectMany(s => s.Statistics)
					.Where(e => e.Name.Contains("Test Profile"))
					.Sum(e => e.OpenCount));
		}
	}
}