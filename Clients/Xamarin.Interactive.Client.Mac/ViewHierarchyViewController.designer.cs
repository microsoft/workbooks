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
    [Register ("ViewHierarchyViewController")]
    partial class ViewHierarchyViewController
    {
        [Outlet]
        AppKit.NSSegmentedControl hierarchySelector { get; set; }

        [Outlet]
        Xamarin.Interactive.OutlineView.CollectionOutlineView outlineView { get; set; }

        [Action ("HierarchySelectionChanged:")]
        partial void HierarchySelectionChanged (Foundation.NSObject sender);

        void ReleaseDesignerOutlets ()
        {
            if (hierarchySelector != null) {
                hierarchySelector.Dispose ();
                hierarchySelector = null;
            }

            if (outlineView != null) {
                outlineView.Dispose ();
                outlineView = null;
            }
        }
    }
}
