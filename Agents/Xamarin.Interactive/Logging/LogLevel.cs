//
// LogLevel.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Logging
{
    [Serializable]
    public enum LogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}