// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.Interactive.Preferences
{
    [Register ("PreferencesDeveloperViewController")]
    partial class PreferencesDeveloperViewController
    {
        [Outlet]
        AppKit.NSPopUpButton inspectorPanePopupButton { get; set; }
        
        void ReleaseDesignerOutlets ()
        {
            if (inspectorPanePopupButton != null) {
                inspectorPanePopupButton.Dispose ();
                inspectorPanePopupButton = null;
            }
        }
    }
}
