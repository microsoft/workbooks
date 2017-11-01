//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Editor
{
    [Flags]
    enum KeyModifier
    {
        None = 0 << 0,
        Mod = 1 << 0,
        Meta = 1 << 1,
        Ctrl = 1 << 2,
        Alt = 1 << 3,
        Shift = 1 << 4
    }
}