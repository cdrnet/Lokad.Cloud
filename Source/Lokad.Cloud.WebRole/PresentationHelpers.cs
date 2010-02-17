#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Web
{
	// TODO (ruegg, 200912): Move to Lokad.Shared

	internal static class PresentationHelpers
	{
		public static string PrettyFormat(this TimeSpan timeSpan)
		{
			const int second = 1;
			const int minute = 60 * second;
			const int hour = 60 * minute;
			const int day = 24 * hour;
			const int month = 30 * day;

			double delta = timeSpan.TotalSeconds;

			if (delta < 1) return timeSpan.Milliseconds + " ms";
			if (delta < 1 * minute) return timeSpan.Seconds == 1 ? "one second" : timeSpan.Seconds + " seconds";
			if (delta < 2 * minute) return "a minute";
			if (delta < 50 * minute) return timeSpan.Minutes + " minutes";
			if (delta < 70 * minute) return "an hour";
			if (delta < 2 * hour) return (int)timeSpan.TotalMinutes + " minutes";
			if (delta < 24 * hour) return timeSpan.Hours + " hours";
			if (delta < 48 * hour) return (int)timeSpan.TotalHours + " hours";
			if (delta < 30 * day) return timeSpan.Days + " days";

			if (delta < 12 * month)
			{
				var months = (int)Math.Floor(timeSpan.Days / 30.0);
				return months <= 1 ? "one month" : months + " months";
			}

			var years = (int)Math.Floor(timeSpan.Days / 365.0);
			return years <= 1 ? "one year" : years + " years";
		}

		public static string PrettyFormatRelativeToNow(this DateTimeOffset dateTime)
		{
			var now = DateTimeOffset.UtcNow;

			if (dateTime.UtcTicks == 0)
			{
				return String.Empty;
			}

			if (dateTime >= now)
			{
				return "just now";
			}

			return PrettyFormat(now - dateTime) + " ago";
		}
	}
}
