//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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