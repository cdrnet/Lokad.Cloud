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
	/// Inherit from this class and override UpdateStatistics()
	/// to load data from non-default counter sources.
	/// </remarks>
	public class ExecutionProfilingMonitor
	{
		readonly IBlobStorageProvider _provider;

		public ExecutionProfilingMonitor(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		public IEnumerable<ExecutionProfilingStatistics> GetStatistics()
		{
			return _provider
				.List(ExecutionProfilingStatisticsName.GetPrefix())
				.Select(name => _provider.GetBlobOrDelete(name))
				.Where(s => null != s);
		}

		/// <remarks>
		/// Base implementation collects default counters of this worker
		/// </remarks>
		public virtual void UpdateStatistics()
		{
			var counters = ExecutionCounters.Default;
			var data = counters.ToList().ToArray().ToPersistence();
			counters.ResetAll();

			Update("Default", d => Aggregate(d.Append(data).ToArray()));
		}

		/// <summary>
		/// Build a blob and put it to the storage
		/// </summary>
		protected void Update(string contextName, Func<ExecutionData[],ExecutionData[]> updater)
		{
			ExecutionProfilingStatistics result;
			_provider.AtomicUpdate(
				ExecutionProfilingStatisticsName.New(contextName),
				s =>
				{
					if (s == null)
					{
						return new ExecutionProfilingStatistics()
						{
							Name = contextName,
							Statistics = updater(new ExecutionData[] { })
						};
					}

					s.Statistics = updater(s.Statistics);
					return s;
				},
				out result
				);
		}

		protected ExecutionData[] Aggregate(IEnumerable<ExecutionData> data)
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
