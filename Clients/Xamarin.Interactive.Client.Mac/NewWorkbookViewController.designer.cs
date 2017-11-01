// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.Interactive.Client.Mac
{
    [Register ("NewWorkbookViewController")]
    partial class NewWorkbookViewController
    {
        [Outlet]
        AppKit.NSButton cancelButton { get; set; }

        [Outlet]
        AppKit.NSStackView featuresStackView { get; set; }

        [Outlet]
        AppKit.NSImageView flairImageView { get; set; }

        [Outlet]
        AppKit.NSCollectionView frameworkCollectionView { get; set; }

        [Outlet]
        AppKit.NSPopUpButton frameworkPopupButton { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (cancelButton != null) {
                cancelButton.Dispose ();
                cancelButton = null;
            }

            if (flairImageView != null) {
                flairImageView.Dispose ();
                flairImageView = null;
            }

            if (frameworkCollectionView != null) {
                frameworkCollectionView.Dispose ();
                frameworkCollectionView = null;
            }

            if (frameworkPopupButton != null) {
                frameworkPopupButton.Dispose ();
                frameworkPopupButton = null;
            }

            if (featuresStackView != null) {
                featuresStackView.Dispose ();
                featuresStackView = null;
            }
        }
    }
}
