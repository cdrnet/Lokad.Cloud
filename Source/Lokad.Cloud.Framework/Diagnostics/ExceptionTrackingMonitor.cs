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
	/// Generic Tracked Exception Monitoring Data Provider
	/// </summary>
	/// <remarks>
	/// Inherit from this class and override UpdateStatistics()
	/// to load data from non-default counter sources.
	/// </remarks>
	public class ExceptionTrackingMonitor
	{
		readonly IBlobStorageProvider _provider;

		public ExceptionTrackingMonitor(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		public IEnumerable<ExceptionTrackingStatistics> GetStatistics()
		{
			return _provider
				.List(ExceptionTrackingStatisticsName.GetPrefix())
				.Select(name => _provider.GetBlobOrDelete(name))
				.Where(s => null != s);
		}

		/// <remarks>
		/// Base implementation collects default counters of this worker only
		/// </remarks>
		public virtual void UpdateStatistics()
		{
			var counters = ExceptionCounters.Default;
			var data = counters.GetHistory().ToArray().ToPersistence();
			counters.Clear();

			Update("Default", d => Aggregate(d.Append(data).ToArray()));
		}

		/// <summary>
		/// Build a blob and put it to the storage
		/// </summary>
		protected void Update(string contextName, Func<ExceptionData[], ExceptionData[]> updater)
		{
			ExceptionTrackingStatistics result;
			_provider.AtomicUpdate(
				ExceptionTrackingStatisticsName.New(contextName),
				s =>
					{
						if (s == null)
						{
							return new ExceptionTrackingStatistics()
								{
									Name = contextName,
									Statistics = updater(new ExceptionData[] {})
								};
						}

						s.Statistics = updater(s.Statistics);
						return s;
					},
				out result
				);
		}

		protected ExceptionData[] Aggregate(IEnumerable<ExceptionData> data)
		{
			return data
				.GroupBy(
				e => e.Text,
				(text, items) =>
					{
						var first = items.First();
						return new ExceptionData
							{
								ID = first.ID,
								Count = items.Sum(c => c.Count),
								Name = first.Name,
								Message = first.Message,
								Text = text
							};
					})
				.OrderByDescending(e => e.Count)
				.Take(50)
				.ToArray();
		}
	}
}
