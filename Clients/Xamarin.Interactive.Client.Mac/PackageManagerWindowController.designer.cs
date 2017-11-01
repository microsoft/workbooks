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
    [Register ("PackageManagerWindowController")]
    partial class PackageManagerWindowController
    {
        [Outlet]
        AppKit.NSButton addButton { get; set; }

        [Outlet]
        AppKit.NSButton cancelButton { get; set; }

        [Outlet]
        AppKit.NSPopUpButton packageSourcesPopUpButton { get; set; }

        [Outlet]
        AppKit.NSButton preReleaseCheckButton { get; set; }

        [Outlet]
        AppKit.NSProgressIndicator progressIndicator { get; set; }

        [Outlet]
        AppKit.NSButton searchButton { get; set; }

        [Outlet]
        AppKit.NSTextField searchField { get; set; }

        [Outlet]
        AppKit.NSTableView searchResultsTableView { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (addButton != null) {
                addButton.Dispose ();
                addButton = null;
            }

            if (cancelButton != null) {
                cancelButton.Dispose ();
                cancelButton = null;
            }

            if (preReleaseCheckButton != null) {
                preReleaseCheckButton.Dispose ();
                preReleaseCheckButton = null;
            }

            if (progressIndicator != null) {
                progressIndicator.Dispose ();
                progressIndicator = null;
            }

            if (searchButton != null) {
                searchButton.Dispose ();
                searchButton = null;
            }

            if (searchField != null) {
                searchField.Dispose ();
                searchField = null;
            }

            if (searchResultsTableView != null) {
                searchResultsTableView.Dispose ();
                searchResultsTableView = null;
            }

            if (packageSourcesPopUpButton != null) {
                packageSourcesPopUpButton.Dispose ();
                packageSourcesPopUpButton = null;
            }
        }
    }
}
