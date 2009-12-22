#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Text.RegularExpressions;

namespace Lokad.Cloud.Web
{
	// TODO (ruegg, 200912): Move to Lokad.Shared

	internal static class PresentationHelpers
	{
		public static string PrettyFormat(this TimeSpan timeSpan)
		{
			double delta = timeSpan.TotalSeconds;

			const int second = 1;
			const int minute = 60 * second;
			const int hour = 60 * minute;
			const int day = 24 * hour;
			const int month = 30 * day;

			if (delta < 1 * minute) return timeSpan.Seconds == 1 ? "one second" : timeSpan.Seconds + " seconds";
			if (delta < 2 * minute) return "a minute";
			if (delta < 45 * minute) return timeSpan.Minutes + " minutes";
			if (delta < 90 * minute) return "an hour";
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
			var now = DateTimeOffset.Now;

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

		public static string PrettyFormatMemoryMB(long byteCount)
		{
			return String.Format("{0} MB", byteCount / (1024 * 1024));
		}

		public static string PrettyFormatMemoryKB(long byteCount)
		{
			return String.Format("{0} KB", byteCount / (1024));
		}

		public static string PrettyFormatOperatingSystem(string os)
		{
			if(string.IsNullOrEmpty(os))
			{
				return string.Empty;
			}

			os = os.Replace("Microsoft Windows ", string.Empty);
			return os.Replace("Service Pack ", "SP");
		}
	}
}
