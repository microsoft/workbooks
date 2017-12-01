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
using Foundation;
using Xamarin.Interactive.Client.ViewInspector;
using Xamarin.Interactive.OutlineView;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class ViewHierarchyViewController : ViewInspectorViewController
    {
        InspectorOutlineViewDelegate outlineViewDelegate;
        CollectionOutlineViewDataSource dataSource;

        ViewHierarchyViewController (IntPtr handle) : base (handle)
        {
            BindData ();
        }

        public override void ViewDidLoad ()
        {
            UpdateSupportedHierarchies (null, null);
            base.ViewDidLoad ();
        }

        public void UpdateSupportedHierarchies (string [] hierarchies, string selectedHierarchy)
        {
            if (hierarchies == null || hierarchies.Length < 2) {
                hierarchySelector.SegmentCount = 0;
                hierarchySelector.Hidden = true;
                return;
            }

            hierarchySelector.SegmentCount = hierarchies.Length;

            for (int i = 0; i < hierarchies.Length; i++) {
                if (selectedHierarchy == hierarchies [i])
                    hierarchySelector.SetSelected (true, i);
                hierarchySelector.SetLabel (hierarchies [i], i);
                hierarchySelector.SetWidth (0, i);
            }

            hierarchySelector.Hidden = false;
        }

        partial void HierarchySelectionChanged (NSObject sender)
        {
            var realSender = (NSSegmentedControl)sender;
            var selectedSegment = realSender.SelectedSegment;
            ParentViewController.UpdateSelectedHierarchy (realSender.GetLabel (selectedSegment));
        }

        void BindData ()
        {
            if (Tree?.RootNode == null)
                return;

            try {
                dataSource = new CollectionOutlineViewDataSource (Tree?.RootNode);
                dataSource.Reload += (sender, e) => {
                    if (e.Item == null)
                        outlineView.ReloadData ();
                    else
                        outlineView.ReloadItem (e.Item, e.ReloadChildren);
                };

                outlineView.DataSource = dataSource;
                outlineView.ReloadData ();
                outlineViewDelegate = new InspectorOutlineViewDelegate (this);
                outlineView.Delegate = outlineViewDelegate;
            } finally {
                if (outlineViewDelegate != null)
                    outlineViewDelegate.InhibitSelectionDidChange = false;
            }
        }

        protected override void OnRootNodeChanged ()
        {
            BindData ();
        }

        class InspectorOutlineViewDelegate : CollectionOutlineViewDelegate
        {
            readonly ViewHierarchyViewController viewController;

            public bool InhibitSelectionDidChange { get; set; }

            public InspectorOutlineViewDelegate (ViewHierarchyViewController viewController) : base (viewController.outlineView) 
                => this.viewController = viewController;

            public override bool SelectionShouldChange (NSOutlineView outlineView)
                => viewController.Session.Agent.IsConnected;

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
                if (InhibitSelectionDidChange)
                    return;
                
                var treeNode = node as InspectTreeNode;
                viewController.Tree.SelectedNode = treeNode;
            }

            public override IEnumerable<NSMenuItem> ContextMenuItemsForNode (TreeNode node)
            {
                if (node.Commands == null)
                    return Array.Empty<NSMenuItem> ();

                return node.Commands.Select (c => new RoutedUICommandMenuItem (c, node));
            }
        }
    }
}
