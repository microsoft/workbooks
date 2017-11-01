//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.OutlineView;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class WorkbookOutlineViewController : SessionViewController
    {
        WorkbookOutlineViewDelegate outlineViewDelegate;
        CollectionOutlineViewDataSource outlineViewDataSource;

        WorkbookOutlineViewController (IntPtr handle) : base (handle)
        {
        }

        protected override void OnSessionAvailable ()
        {
            outlineView.RowSizeStyle = NSTableViewRowSizeStyle.Medium;

            outlineViewDataSource = new CollectionOutlineViewDataSource (Session.Workbook.TreeNode);
            outlineViewDataSource.Reload += (sender, e) => {
                var selectedNode = outlineView.SelectedNode;

                if (e.Item == null)
                    outlineView.ReloadData ();
                else
                    outlineView.ReloadItem (e.Item, e.ReloadChildren);

                if (selectedNode != null)
                    outlineView.SelectedNode = selectedNode;
            };

            outlineView.DataSource = outlineViewDataSource;
            outlineView.ReloadData ();

            outlineViewDelegate = new WorkbookOutlineViewDelegate (outlineView);
            outlineViewDelegate.NavigateTableOfContents += toc =>
                Session.WorkbookPageView.ScrollToElementWithId (toc.Id);
            outlineView.Delegate = outlineViewDelegate;
        }

        protected override void OnSessionTitleUpdated ()
        {
            outlineView.ReloadData ();
        }

        [Export ("addItem:")]
        void AddItem (NSObject sender)
        {
            var button = (NSButton)sender;
            addItemMenu.PopUpMenu (
                null,
                new CoreGraphics.CGPoint (0, button.Frame.Height),
                button);
        }
    }
}