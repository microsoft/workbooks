//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using AppKit;

using Xamarin.Interactive.OutlineView;
using Xamarin.Interactive.TreeModel;
using Xamarin.Interactive.Workbook.Structure;

namespace Xamarin.Interactive.Client.Mac
{
    class WorkbookOutlineViewDelegate : CollectionOutlineViewDelegate
    {
        public event Action<TableOfContentsNode> NavigateTableOfContents;

        public WorkbookOutlineViewDelegate (CollectionOutlineView outlineView) : base (outlineView)
        {
        }

        public override NSView GetView (
            NSTableColumn tableColumn,
            CollectionOutlineViewDataSource.NodeProxy nodeProxy)
        {
            var view = (Views.XIIconThemeOutlineCellView)OutlineView.MakeView (
                tableColumn.Identifier,
                this);
            view.IconName = nodeProxy.Node.IconName;
            return view;
        }

        public override void SelectionDidChange (TreeNode node)
        {
            var tocNode = node as TableOfContentsNode;
            if (tocNode != null)
                NavigateTableOfContents?.Invoke (tocNode);
        }

        public override IEnumerable<NSMenuItem> ContextMenuItemsForNode (TreeNode node)
        {
            if (node.Commands == null)
                return Array.Empty<NSMenuItem> ();

            return node.Commands.Select (c => new RoutedUICommandMenuItem (c, node));
        }
    }
}