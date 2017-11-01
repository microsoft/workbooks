// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.Interactive.Client.Mac
{
    [Register ("LogWindowController")]
    partial class LogWindowController
    {
        [Outlet]
        AppKit.NSPopUpButton logOwnerPopUpButton { get; set; }

        [Outlet]
        AppKit.NSSearchField logSearchField { get; set; }

        [Outlet]
        AppKit.NSTableView logTableView { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (logOwnerPopUpButton != null) {
                logOwnerPopUpButton.Dispose ();
                logOwnerPopUpButton = null;
            }

            if (logSearchField != null) {
                logSearchField.Dispose ();
                logSearchField = null;
            }

            if (logTableView != null) {
                logTableView.Dispose ();
                logTableView = null;
            }
        }
    }
}
