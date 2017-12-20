//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
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
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.OutlineView;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class ViewHierarchyViewController : ViewInspectorViewController
    {
        const string TAG = nameof (ViewHierarchyViewController);

        InspectorOutlineViewDelegate outlineViewDelegate;
        CollectionOutlineViewDataSource dataSource;

        ViewHierarchyViewController (IntPtr handle) : base (handle)
            => BindData ();

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
                var oldSource = dataSource;

                dataSource = new InspectorOutlineViewDataSource (Tree);
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

                oldSource?.Dispose ();
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
        }

        protected override void OnRootNodeChanged () => BindData ();

        class InspectorOutlineViewDataSource : CollectionOutlineViewDataSource 
        {
            InspectTreeRoot Tree { get; }

            public InspectorOutlineViewDataSource (InspectTreeRoot tree) : base (tree.RootNode)
                => Tree = tree;

            public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
            {
                if (item == null)
                    return (nint)Tree.Count;

                var children = (item as NodeProxy)?.Node?.Children;
                if (children != null)
                    return children.Count;

                return 0;
            }

            public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
            {
                TreeNode node;
                if (item == null) {
                    node = Tree [(int)childIndex];
                    return BindOutlineViewNode (node);
                }

                node = (item as NodeProxy)?.Node;
                if (node != null) {
                    var child = node.Children [(int)childIndex];
                    return BindOutlineViewNode (child);
                }

                return null;
            }
        }

        class InspectorOutlineViewDelegate : CollectionOutlineViewDelegate
        {
            readonly ViewHierarchyViewController viewController;

            public InspectorOutlineViewDelegate (ViewHierarchyViewController viewController) : base (viewController.outlineView) 
                => this.viewController = viewController;

            public override bool SelectionShouldChange (NSOutlineView outlineView)
                => viewController.Session.Agent.IsConnected;

            public override void SelectionDidChange (TreeNode node)
                => viewController.Tree.SelectedNode = (node as InspectTreeNode);

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

            public override IEnumerable<NSMenuItem> ContextMenuItemsForNode (TreeNode node)
            {
                if (node.Commands == null)
                    return Array.Empty<NSMenuItem> ();

                return node.Commands.Select (c => new RoutedUICommandMenuItem (c, node));
            }
        }
    }
}
