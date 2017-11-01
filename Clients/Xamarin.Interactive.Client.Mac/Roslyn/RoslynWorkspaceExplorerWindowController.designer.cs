// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.Interactive.Client.Mac.Roslyn
{
	[Register ("RoslynWorkspaceExplorerWindowController")]
	partial class RoslynWorkspaceExplorerWindowController
	{
		[Outlet]
		AppKit.NSOutlineView outlineView { get; set; }

		[Outlet]
		AppKit.NSOutlineView syntaxOutlineView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (outlineView != null) {
				outlineView.Dispose ();
				outlineView = null;
			}

			if (syntaxOutlineView != null) {
				syntaxOutlineView.Dispose ();
				syntaxOutlineView = null;
			}
		}
	}
}
