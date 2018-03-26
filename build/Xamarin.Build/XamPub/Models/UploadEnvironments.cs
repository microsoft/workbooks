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
    enum UploadEnvironments
    {
        None = 0 << 0,
        DMS = 1 << 0,
        ROQ = 1 << 1,
        Willow = 1 << 2,
        XamarinUpdater = 1 << 3,
        XamarinInstaller = 1 << 4,
        XVSFeed = 1 << 5,
    }
}