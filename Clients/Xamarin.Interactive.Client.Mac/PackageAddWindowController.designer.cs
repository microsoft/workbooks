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
    [Register ("PackageAddWindowController")]
    partial class PackageAddWindowController
    {
        [Outlet]
        AppKit.NSButton cancelButton { get; set; }

        [Outlet]
        AppKit.NSProgressIndicator progressIndicator { get; set; }

        [Outlet]
        AppKit.NSTextField statusTextField { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (progressIndicator != null) {
                progressIndicator.Dispose ();
                progressIndicator = null;
            }

            if (statusTextField != null) {
                statusTextField.Dispose ();
                statusTextField = null;
            }

            if (cancelButton != null) {
                cancelButton.Dispose ();
                cancelButton = null;
            }
        }
    }
}
