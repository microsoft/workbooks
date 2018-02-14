// Authors:
//   Larry Ewing <lewing@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Client.ViewInspector
{
    [Flags]
    enum DisplayMode
    {
        None = 0,
        Frames = 1,
        Content = 1 << 1,
        FramesAndContent = Frames | Content
    }
}
