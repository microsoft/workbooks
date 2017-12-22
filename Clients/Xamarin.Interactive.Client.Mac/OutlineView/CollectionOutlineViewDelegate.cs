//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using AppKit;
using Foundation;

using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.OutlineView
{
    abstract class CollectionOutlineViewDelegate : NSOutlineViewDelegate
    {
        public CollectionOutlineView OutlineView { get; }

        TreeNode selectedNode;

        protected CollectionOutlineViewDelegate (CollectionOutlineView outlineView)
        {
            OutlineView = outlineView ?? throw new ArgumentNullException (nameof (outlineView));

            OutlineView.DoubleClick += OutlineView_DoubleClick;
        }

        public virtual IEnumerable<NSMenuItem> ContextMenuItemsForNode (TreeNode node)
        {
            yield break;
        }

        void OutlineView_DoubleClick (object sender, EventArgs e)
        {
            var evnt = NSApplication.SharedApplication.CurrentEvent;
            var coord = OutlineView.ConvertPointFromView (evnt.LocationInWindow, null);
            var row = OutlineView.GetRow (coord);

            if (row < 0)
                return;

            var node = OutlineView.GetNodeProxy (row)?.Node;
            if (node == null)
                return;

            var command = node.DefaultCommand;
            if (command != null && command.CanExecute (null, OutlineView))
                command.Execute (null, OutlineView);
            else if (node.IsRenamable)
                OutlineView.EditColumn (0, row, evnt, true);
        }

        public sealed override void SelectionDidChange (NSNotification notification)
        {
            if (selectedNode != null)
                selectedNode.IsSelected = false;

            selectedNode = (notification.Object as CollectionOutlineView)?.SelectedNode as TreeNode;

            if (selectedNode != null) 
                selectedNode.IsSelected = true;

            SelectionDidChange (selectedNode);
        }

        public virtual void SelectionDidChange (TreeNode node)
        {
        }

        public sealed override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
        {
            var node = (item as CollectionOutlineViewDataSource.NodeProxy)?.Node;
            return node != null && node.IsSelectable;
        }

        public sealed override void ItemDidExpand (NSNotification notification)
            => UpdateItemExpansion (notification, true);

        public sealed override void ItemDidCollapse (NSNotification notification)
            => UpdateItemExpansion (notification, false);

        static readonly NSString NSObjectKey = new NSString ("NSObject");

        void UpdateItemExpansion (NSNotification notification, bool expanded)
        {
            NSObject o;
            if (notification.UserInfo.TryGetValue (NSObjectKey, out o)) {
                var proxy = o as CollectionOutlineViewDataSource.NodeProxy;
                if (proxy != null)
                    proxy.Node.IsExpanded = expanded;
            }
        }

        public sealed override NSView GetView (
            NSOutlineView outlineView,
            NSTableColumn tableColumn,
            NSObject item)
        {
            var nodeProxy = (CollectionOutlineViewDataSource.NodeProxy)item;
            var node = nodeProxy.Node;

            var rawView = GetView (tableColumn, nodeProxy);

            var view = rawView as NSTableCellView;
            if (view == null)
                return rawView;

            view.TextField.StringValue = node.Name ?? string.Empty;
            view.ToolTip = node.ToolTip ?? string.Empty;

            view.TextField.FocusRingType = NSFocusRingType.None;
            view.TextField.Editable = node.IsRenamable;
            view.TextField.Action = CollectionOutlineViewDataSource.NodeProxy.RenameSelector;
            view.TextField.Target = nodeProxy;

            return view;
        }

        public abstract NSView GetView (
            NSTableColumn tableColumn,
            CollectionOutlineViewDataSource.NodeProxy nodeProxy);

        public sealed override NSTableRowView RowViewForItem (NSOutlineView outlineView, NSObject item)
            => outlineView.MakeView ("row-view", null) as OutlineRowView ?? new OutlineRowView {
                Identifier = "row-view"
            };

        sealed class OutlineRowView : NSTableRowView
        {
            public override bool Emphasized {
                get { return true; }
                set { }
            }
        }
    }
}