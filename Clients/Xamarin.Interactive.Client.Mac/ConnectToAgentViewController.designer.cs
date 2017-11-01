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
    [Register ("ConnectToAgentViewController")]
    partial class ConnectToAgentViewController
    {
        [Outlet]
        AppKit.NSTextField clientSessionUriTextField { get; set; }

        [Outlet]
        AppKit.NSButton connectButton { get; set; }

        [Outlet]
        AppKit.NSButton liveInspectionRadioButton { get; set; }

        [Outlet]
        AppKit.NSTextField locationTextField { get; set; }

        [Outlet]
        AppKit.NSButton workbookRadioButton { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (connectButton != null) {
                connectButton.Dispose ();
                connectButton = null;
            }

            if (liveInspectionRadioButton != null) {
                liveInspectionRadioButton.Dispose ();
                liveInspectionRadioButton = null;
            }

            if (locationTextField != null) {
                locationTextField.Dispose ();
                locationTextField = null;
            }

            if (workbookRadioButton != null) {
                workbookRadioButton.Dispose ();
                workbookRadioButton = null;
            }

            if (clientSessionUriTextField != null) {
                clientSessionUriTextField.Dispose ();
                clientSessionUriTextField = null;
            }
        }
    }
}
