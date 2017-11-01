//
// TimeSpanExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive
{
	static class TimeSpanExtensions
	{
		public static string ToPerformanceTimeString (this TimeSpan span, string format = "G3")
		{
			if (span.Seconds > 0)
				return span.TotalSeconds.ToString (format) + "s";
			else if (span.Milliseconds > 0)
				return span.TotalMilliseconds.ToString (format) + "ms";
			else
				return (span.Ticks / 10.0).ToString (format) + "μs";
		}
	}
}