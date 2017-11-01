//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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