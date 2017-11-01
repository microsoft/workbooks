//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    static class Entry
    {
        static int Main (string [] args)
        {
            NSApplication.Init ();

            if (args.Length > 0 && args [0] == "cli") {
                ObjCRuntime.Runtime.ChangeToOriginalWorkingDirectory ();

                var exitCode = CommandLineTool.Entry.Run (
                    args.Skip (1).ToArray (),
                    out var shouldExit);

                if (shouldExit)
                    return exitCode;

                args = Array.Empty<string> ();
            }

            NSApplication.Main (args);

            return 0;
        }
    }
}