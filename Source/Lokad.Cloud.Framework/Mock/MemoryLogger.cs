#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;

namespace Lokad.Cloud.Mock
{
	public class MemoryLogger : ILog
	{
		public bool IsEnabled(LogLevel level)
		{
			return false;
		}

		public void Log(LogLevel level, Exception ex, object message)
		{
			//do nothing
		}

		public void Log(LogLevel level, object message)
		{
			Log(level, null, message);
		}
	}
}
