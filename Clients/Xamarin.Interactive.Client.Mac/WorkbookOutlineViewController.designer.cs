// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

using Xamarin.Interactive.OutlineView;

namespace Xamarin.Interactive.Client.Mac
{
	[Register ("WorkbookOutlineViewController")]
	partial class WorkbookOutlineViewController
	{
		[Outlet]
		AppKit.NSMenu addItemMenu { get; set; }

		[Outlet]
		CollectionOutlineView outlineView { get; set; }

		[Action ("addItem:")]
		partial void addItem (Foundation.NSObject sender);

		void ReleaseDesignerOutlets ()
		{
			if (outlineView != null) {
				outlineView.Dispose ();
				outlineView = null;
			}

			if (addItemMenu != null) {
				addItemMenu.Dispose ();
				addItemMenu = null;
			}
		}
	}
}
