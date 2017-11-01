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
    [Flags]
    enum LogFlags
    {
        None = 0 << 0,
        NoFlair = 1 << 0,
        SkipTelemetry = 1 << 1
    }
}