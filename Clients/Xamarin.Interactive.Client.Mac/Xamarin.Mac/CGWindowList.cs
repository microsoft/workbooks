//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

using ObjCRuntime;
using Foundation;

namespace CoreGraphics
{
    [Flags]
    enum CGWindowListOptions : uint
    {
        All = 0,
        OnScreenOnly = 1 << 0,
        OnScreenAboveWindow = 1 << 1,
        OnScreenBelowWindow = 1 << 2,
        IncludingWindow = 1 << 3,
        ExcludeDesktopElements = 1 << 4
    }

    static class CGWindowList
    {
        [DllImport (Constants.CoreGraphicsLibrary)]
        static extern IntPtr CGWindowListCopyWindowInfo (CGWindowListOptions options, uint relativeToWindowId);

        public static NSDictionary [] CopyWindowInfo (CGWindowListOptions options, uint relativeToWindowId)
        {
            return NSArray.ArrayFromHandle<NSDictionary> (CGWindowListCopyWindowInfo (options, relativeToWindowId));
        }
    }
}