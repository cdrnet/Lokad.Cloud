#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Diagnostics
{
	public enum TimeSegmentPeriod
	{
		Day,
		Month
	}

	/// <remarks>
	/// Generated time segments are strictly ordered ascending by time and date
	/// when compared as string.
	/// </remarks>
	static class TimeSegments
	{
		public const string DayPrefix = "day";
		public const string MonthPrefix = "month";

		public static string Day(DateTimeOffset timestamp)
		{
			return For(TimeSegmentPeriod.Day, timestamp);
		}

		public static string Month(DateTimeOffset timestamp)
		{
			return For(TimeSegmentPeriod.Month, timestamp);
		}

		public static string For(TimeSegmentPeriod period, DateTimeOffset timestamp)
		{
			var utcDate = timestamp.UtcDateTime;
			return String.Format(GetPeriodFormatString(period), utcDate);
		}

		static string GetPeriodFormatString(TimeSegmentPeriod period)
		{
			switch (period)
			{
				case TimeSegmentPeriod.Day:
					return "day-{0:yyyyMMdd}";
				case TimeSegmentPeriod.Month:
					return "month-{0:yyyyMM}";
				default:
					throw new ArgumentOutOfRangeException("period");
			}
		}
	}
}
