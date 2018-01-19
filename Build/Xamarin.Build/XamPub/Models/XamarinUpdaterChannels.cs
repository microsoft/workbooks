//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.XamPub.Models
{
    [Flags]
    enum XamarinUpdaterChannels
    {
        None = 0 << 0,
        Stable = 1 << 0,
        Beta = 1 << 1,
        Alpha = 1 << 2,
        Test = 1 << 3
    }
}