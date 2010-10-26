#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Diagnostics;
using Lokad.Diagnostics.Persist;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Generic Execution Profile Monitoring Data Provider
	/// </summary>
	/// <remarks>
	/// Implement <see cref="ICloudDiagnosticsSource"/> 
	/// to provide data from non-default counter sources.
	/// </remarks>
	internal class ExecutionProfilingMonitor
	{
		readonly ICloudDiagnosticsRepository _repository;

		/// <summary>
		/// Creates an instance of the <see cref="ExecutionProfilingMonitor"/> class.
		/// </summary>
		public ExecutionProfilingMonitor(ICloudDiagnosticsRepository repository)
		{
			_repository = repository;
		}

		/// <remarks>
		/// Base implementation collects default counters of this worker
		/// </remarks>
		public void UpdateDefaultStatistics()
		{
			var counters = ExecutionCounters.Default;
			var data = counters.ToList().ToArray().ToPersistence();
			counters.ResetAll();

			Update("Default", data);
		}

		/// <summary>
		/// Update the statistics data with a set of additional new data items
		/// </summary>
		public void Update(string contextName, IEnumerable<ExecutionData> additionalData)
		{
			var dataList = additionalData.ToList();
			if(dataList.Count == 0)
			{
				return;
			}

			var timestamp = DateTimeOffset.UtcNow;
			Update(TimeSegments.Day(timestamp), contextName, dataList);
			Update(TimeSegments.Month(timestamp), contextName, dataList);
		}

		/// <summary>
		/// Remove statistics older than the provided time stamp.
		/// </summary>
		public void RemoveStatisticsBefore(DateTimeOffset before)
		{
			_repository.RemoveExecutionProfilingStatistics(TimeSegments.DayPrefix, TimeSegments.Day(before));
			_repository.RemoveExecutionProfilingStatistics(TimeSegments.MonthPrefix, TimeSegments.Month(before));
		}

		/// <summary>
		/// Remove statistics older than the provided number of periods (0 removes all but the current period).
		/// </summary>
		public void RemoveStatisticsBefore(int numberOfPeriods)
		{
			var now = DateTimeOffset.UtcNow;

			_repository.RemoveExecutionProfilingStatistics(
				TimeSegments.DayPrefix,
				TimeSegments.Day(now.AddDays(-numberOfPeriods)));

			_repository.RemoveExecutionProfilingStatistics(
				TimeSegments.MonthPrefix,
				TimeSegments.Month(now.AddMonths(-numberOfPeriods)));
		}

		/// <summary>
		/// Update the statistics data with a set of additional new data items
		/// </summary>
		public void Update(string timeSegment, string contextName, IEnumerable<ExecutionData> additionalData)
		{
			_repository.UpdateExecutionProfilingStatistics(
				timeSegment,
				contextName,
				s =>
					{
						if (!s.HasValue)
						{
							return new ExecutionProfilingStatistics()
								{
									Name = contextName,
									Statistics = additionalData.ToArray()
								};
						}

						var stats = s.Value;
						stats.Statistics = Aggregate(stats.Statistics.Append(additionalData)).ToArray();
						return stats;
					});
		}

		/// <summary>
		/// Aggregation Helper
		/// </summary>
		private ExecutionData[] Aggregate(IEnumerable<ExecutionData> data)
		{
			return data
				.GroupBy(
				p => p.Name,
				(name, items) => new ExecutionData
					{
						Name = name,
						OpenCount = items.Sum(c => c.OpenCount),
						CloseCount = items.Sum(c => c.CloseCount),
						Counters = TotalCounters(items),
						RunningTime = items.Sum(c => c.RunningTime)
					})
				.OrderBy(e => e.Name)
				.ToArray();
		}

		static long[] TotalCounters(IEnumerable<ExecutionData> data)
		{
			if (data.Count() == 0)
			{
				return new long[0];
			}

			var length = data.First().Counters.Length;
			var counters = new long[length];

			foreach (var stat in data)
			{
				if (stat.Counters.Length != length)
				{
					// name/version collision
					return new long[] { -1, -1, -1 };
				}

				for (int i = 0; i < length; i++)
				{
					counters[i] += stat.Counters[i];
				}
			}

			return counters;
		}
	}
}
