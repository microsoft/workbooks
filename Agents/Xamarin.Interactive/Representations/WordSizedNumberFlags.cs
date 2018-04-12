//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Representations
{
    [Flags]
    enum WordSizedNumberFlags : byte
    {
        None = 0,
        Signed = 1,
        Real = 2,
        Pointer = 4
    }
}