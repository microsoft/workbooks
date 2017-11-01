//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;

using AppKit;
using Foundation;
using ObjCRuntime;

using Xamarin.Interactive.Client.Mac.ViewInspector;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class ViewInspectorMainViewController : SessionSplitViewController, INSMenuValidation
    {
        InspectView representedRootView;
        string selectedHierarchy;

        ViewInspectorMainViewController (IntPtr handle) : base (handle)
        {
        }

        protected override void OnAgentFeaturesUpdated ()
            => RefreshVisualTree (this);

        bool isVisualTreeRefreshing;

        async Task RefreshVisualTreeAsync ()
        {
            InspectView remoteView = null;
            string [] supportedHierarchies = null;

            isVisualTreeRefreshing = true;

            try {
                supportedHierarchies = Session.Agent?.Features?.SupportedViewInspectionHierarchies;

                if (supportedHierarchies != null && supportedHierarchies.Length > 0) {
                    if (!supportedHierarchies.Contains (selectedHierarchy))
                        selectedHierarchy = supportedHierarchies [0];

                    remoteView = await Session.Agent.Api.GetVisualTreeAsync (
                        selectedHierarchy,
                        captureViews: true);
                } else {
                    supportedHierarchies = null;
                    selectedHierarchy = null;
                }
            } catch (Exception e) {
                e.ToUserPresentable (Catalog.GetString ("Error trying to inspect remote view"))
                    .Present (this);
            }

            SelectView (remoteView, withinExistingTree: false, setSelectedView: false);

            UpdateHierarchiesList (supportedHierarchies, selectedHierarchy);

            isVisualTreeRefreshing = false;

            View?.Window?.Toolbar?.ValidateVisibleItems ();
        }

        public void UpdateSelectedHierarchy (string selectedHierarchy)
        {
            this.selectedHierarchy = selectedHierarchy;
            RefreshVisualTree (this);
        }

        public void UpdateHierarchiesList (string [] supportedHierarchies, string selectedHierarchy)
        {
            ChildViewControllers.OfType<ViewHierarchyViewController> ().ForEach (vc => {
                vc.UpdateSupportedHierarchies (supportedHierarchies, selectedHierarchy);
            });
        }

        public void SelectView (InspectView view)
            => SelectView (view, false, true);

        void SelectView (InspectView view, bool withinExistingTree, bool setSelectedView)
        {
            if (!Session.Agent.IsConnected)
                return;

            if (setSelectedView
                && !string.IsNullOrEmpty (view?.PublicCSharpType)
                && Session.SessionKind == ClientSessionKind.LiveInspection) {
                var code = $"var selectedView = GetObject<{view.PublicCSharpType}> (0x{view.Handle:x})";
                Session.WorkbookPageView.EvaluateAsync (code).Forget ();
            }

            if (withinExistingTree && representedRootView != null && view != null)
                view = representedRootView.FindSelfOrChild (v => v.Handle == view.Handle);

            if (view != null && view.Root == view && view.IsFakeRoot)
                view = view.Children.FirstOrDefault () ?? view;

            var root = view?.Root;
            var current = view;

            // find the "window" to represent in the 3D view by either
            // using the root node of the tree for trees with real roots,
            // or by walking up to find the real root below the fake root
            if (root != null &&
                root.IsFakeRoot &&
                current != root) {
                while (current.Parent != null && current.Parent != root)
                    current = current.Parent;
            } else
                current = root;

            representedRootView = root;
            ChildViewControllers.OfType<ViewInspectorViewController> ().ForEach (viewController => {
                viewController.RootView = root;
                viewController.RepresentedView = current;
                viewController.SelectedView = view;
            });
        }

        public bool ValidateMenuItem (NSMenuItem item)
        {
            if (item.Action?.Name == switchDisplayModeSel) {
                // We're asking to validate state for the switch display mode buttons.
                // We'll need to grab the current display mode off the scene view.
                var visualRepController = ChildViewControllers
                    .OfType<VisualRepViewController> ()
                    .FirstOrDefault ();
                var sceneView = visualRepController.scnView;
                var currentDisplayMode = sceneView.DisplayMode;

                if ((DisplayMode)(int)item.Tag == currentDisplayMode)
                    item.State = NSCellStateValue.On;
                else
                    item.State = NSCellStateValue.Off;
            }

            if (item.Action?.Name == switchDisplayDepthSel) {
                var visualRepController = ChildViewControllers
                    .OfType<VisualRepViewController> ()
                    .FirstOrDefault ();
                var currentViewDepth = visualRepController.ViewDepth;

                if ((ViewDepth)(int)item.Tag == currentViewDepth)
                    item.State = NSCellStateValue.On;
                else
                    item.State = NSCellStateValue.Off;
            }

            if (item.Action?.Name == toggleHiddenSel) {
                var visualRepController = ChildViewControllers
                    .OfType<VisualRepViewController> ()
                    .FirstOrDefault ();
                var sceneView = visualRepController.scnView;

                item.State = sceneView.ShowHiddenViews ? NSCellStateValue.On : NSCellStateValue.Off;
            }

            return item.Action != null ? RespondsToSelector (item.Action) : true;
        }

        #region Command Selectors

        const string refreshVisualTreeSel = "refreshVisualTree:";
        const string inspectViewSel = "inspectView:";
        const string resetCameraSel = "resetCamera:";
        const string switchDisplayDepthSel = "switchDisplayDepth:";
        const string switchDisplayModeSel = "switchDisplayMode:";
        const string toggleHiddenSel = "toggleHidden:";

        public override bool RespondsToSelector (Selector sel)
        {
            switch (sel.Name) {
            case refreshVisualTreeSel:
            case resetCameraSel:
            case switchDisplayDepthSel:
            case switchDisplayModeSel:
            case toggleHiddenSel:
                return Session.Agent.IsConnected && !isVisualTreeRefreshing;
            case inspectViewSel:
                return Session.Agent.IsConnected;
            }

            return base.RespondsToSelector (sel);
        }

        [Export (toggleHiddenSel)]
        void ToggleHiddenViews (NSObject sender)
        {
            var realSender = (NSMenuItem)sender;
            // If it's currently on, turn it off. If it's off, turn it on.
            var newDisplayMode = realSender.State == NSCellStateValue.On ? false : true;
            ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                viewController.scnView.ShowHiddenViews = newDisplayMode;
            });
            realSender.State = newDisplayMode ? NSCellStateValue.On : NSCellStateValue.Off;
        }

        [Export (switchDisplayModeSel)]
        void SwitchDisplayMode (NSObject sender)
        {
            var realSender = (NSMenuItem)sender;
            var newDisplayMode = (DisplayMode)(int)realSender.Tag;
            ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                viewController.scnView.DisplayMode = newDisplayMode;
            });
        }

        [Export (switchDisplayDepthSel)]
        void SwitchDisplayDepth (NSObject sender)
        {
            var realSender = (NSMenuItem)sender;
            var newViewDepth = (ViewDepth)(int)realSender.Tag;
            ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                viewController.ViewDepth = newViewDepth;
            });
        }

        [Export (resetCameraSel)]
        void ResetCamera (NSObject sender)
        {
            ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                viewController.ResetCamera ();
            });
        }

        [Export (refreshVisualTreeSel)]
        void RefreshVisualTree (NSObject sender)
            => RefreshVisualTreeAsync ().Forget ();

        [Export (inspectViewSel)]
        void InspectHighlightedView (NSObject sender)
        {
            if (!Session.Agent.IsConnected)
                return;

            var highlighter = new Highlighter (Session, selectedHierarchy);
            highlighter.ViewSelected += (o, e)
                => InvokeOnMainThread (() => SelectView (e.SelectedView, withinExistingTree: true, setSelectedView: true));

            // sender might not be the toolbar button, but we always want to show
            // the toolbar button as highlighted and recessed during the view selection
            // operation, so find it in the toolbar explicitly.
            var button = View?.Window?.Toolbar?.Items
                ?.FirstOrDefault (item => item.Action?.Name == inspectViewSel)
                ?.View as NSButton;

            if (button != null) {
                var defaultBezelStyle = button.BezelStyle;

                // need to defer to the main loop to allow any in-progress
                // click event to finish processing on the current main loop
                // iteration, otherwise the "Highlighted" effect will be ignored.
                BeginInvokeOnMainThread (() => {
                    button.Highlighted = true;
                    button.BezelStyle = NSBezelStyle.Recessed;
                });

                highlighter.HighlightEnded += (o, e) => {
                    button.Highlighted = false;
                    button.BezelStyle = defaultBezelStyle;
                };
            }

            highlighter.Start ();
        }

        #endregion
    }
}
