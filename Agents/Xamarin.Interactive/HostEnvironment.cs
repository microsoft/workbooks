// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Xamarin.Interactive
{
    static class HostEnvironment
    {
        static HostOS GetHostOS ()
        {
            // Try to do things the "new" way as supported on .NET Core and
            // at least .NET Framework 4.7. These all currently fail on Mono
            // <= 5.8 (2018-03-13), but should hopefully work one day.
            if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
                return HostOS.Windows;
            if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
                return HostOS.macOS;
            if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
                return HostOS.Linux;

            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return HostOS.macOS; // assume Mono on macOS

            // Otherwise assume Windows because we will only be running
            // on .NET Core on Linux, which won't reach here.
            return HostOS.Windows;
        }

        public static readonly HostOS OS = GetHostOS ();
    }
}