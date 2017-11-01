//
// LogFlags.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Logging
{
    [Serializable]
    [Flags]
    enum LogFlags
    {
        None = 0 << 0,
        NoFlair = 1 << 0,
        SkipTelemetry = 1 << 1
    }
}