//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Client.Mac.Menu
{
    [Register ("XIDebugMenuItem")]
    sealed class XIDebugMenuItem : NSMenuItem
    {
        public XIDebugMenuItem (NSCoder coder) : base (coder)
        {
        }

        public XIDebugMenuItem (IntPtr handle) : base (handle)
        {
        }

        public override bool Hidden {
            get {
                return
#if DEBUG
                    false;
#else
                    true;
#endif
            }
            set { }
        }
    }
}
