//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class ViewHierarchyViewController : ViewInspectorViewController
    {
        readonly InspectViewDataSource dataSource;
        readonly OutlineViewDelegate outlineViewDelegate;

        ViewHierarchyViewController (IntPtr handle) : base (handle)
        {
            dataSource = new InspectViewDataSource ();
            outlineViewDelegate = new OutlineViewDelegate (this);
        }

        public override void ViewDidLoad ()
        {
            outlineView.DataSource = dataSource;
            outlineView.Delegate = outlineViewDelegate;
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

        protected override void OnRootViewChanged ()
        {
            try {
                outlineViewDelegate.InhibitSelectionDidChange = true;

                dataSource.Load (RootView);

                outlineView.ReloadData ();
                outlineView.ExpandItem (null, true);
            } finally {
                outlineViewDelegate.InhibitSelectionDidChange = false;
            }
        }

        protected override void OnSelectedViewChanged ()
        {
            try {
                outlineViewDelegate.InhibitSelectionDidChange = true;
                outlineView.SelectItem ((InspectViewPeer)SelectedView.Peer);
            } finally {
                outlineViewDelegate.InhibitSelectionDidChange = false;
            }
        }

        class OutlineViewDelegate : NSOutlineViewDelegate
        {
            readonly ViewHierarchyViewController viewController;

            public bool InhibitSelectionDidChange { get; set; }

            public OutlineViewDelegate (ViewHierarchyViewController viewController)
            {
                this.viewController = viewController;
            }

            public override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
                => !((InspectView)(InspectViewPeer)item).IsFakeRoot;

            public override bool SelectionShouldChange (NSOutlineView outlineView)
                => viewController.Session.Agent.IsConnected;

            public override void SelectionDidChange (NSNotification notification)
            {
                if (InhibitSelectionDidChange)
                    return;

                var row = viewController.outlineView.SelectedRow;
                if (row < 0)
                    row = 0;

                InspectView view = viewController.outlineView.ItemAtRow (row) as InspectViewPeer;
                viewController.ParentViewController.SelectView (view);
            }
        }
    }
}
