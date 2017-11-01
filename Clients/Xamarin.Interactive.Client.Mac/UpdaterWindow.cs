//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
    [Register (nameof (UpdaterWindow))]
    sealed class UpdaterWindow : NSWindow
    {
        UpdaterWindow (IntPtr handle) : base (handle)
        {
            Center ();

            WindowShouldClose = (o) => {
                var viewController = ContentViewController as UpdaterViewController;
                if (!viewController.CanCancel)
                    return true;

                var alert = new NSAlert {
                    AlertStyle = NSAlertStyle.Informational,
                    MessageText = Catalog.GetString ("Cancel Download?"),
                    InformativeText = Catalog.GetString (
                        "The software update is still downloading. " +
                        "Closing the updater will cancel the update.")
                };

                alert.AddButton (Catalog.GetString ("Continue Downloading"));
                alert.AddButton (Catalog.GetString ("Cancel"));

                alert.BeginSheet (this, result => {
                    if ((int)result == 1001) {
                        viewController.Cancel ();
                        Close ();
                    }
                });

                return false;
            };
        }
    }
}