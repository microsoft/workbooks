//
// Authors:
//   Larry Ewing <lewing@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Linq;

using AppKit;
using Foundation;
using ObjCRuntime;

using Xamarin.Interactive.Client.ViewInspector;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class ViewInspectorMainViewController : SessionSplitViewController, INSMenuValidation
    {
        ViewInspectorViewModel model;

        ViewInspectorMainViewController (IntPtr handle) : base (handle)
        {
        }

        protected override void OnSessionAvailable ()
        {
            base.OnSessionAvailable ();
            model = new MacViewInspector (this, Session);
        }

        protected override void OnAgentFeaturesUpdated ()
            => RefreshVisualTree (this);

        public void UpdateSelectedHierarchy (string selectedHierarchy)
            => model.SelectedHierarchy = selectedHierarchy;

        public void SelectView (InspectView view)
            => SelectView (view, false, true);

        void SelectView (InspectView view, bool withinExistingTree = false, bool setSelectedView = true)
            => model.SelectView (view, withinExistingTree, setSelectedView);

        static T TagToEnum<T> (nint tag)
        {
            // Keep these synced to the tags for the 2D/3D menu items in the Main storyboard
            object result = default (T);
            switch (result) {
            case DisplayMode _:
                return (T)(object)(DisplayMode)(uint)(tag - 4);
            case RenderingDepth _:
                return (T)(object)(RenderingDepth)(uint)(tag - 8);
            default:
                return (T)result;
            }
        }

        public bool ValidateMenuItem (NSMenuItem item)
        {
            switch (item.Action?.Name) {
            case switchDisplayModeSel:
                if (TagToEnum<DisplayMode> (item.Tag) == model.DisplayMode)
                    item.State = NSCellStateValue.On;
                else
                    item.State = NSCellStateValue.Off;
                break;
            case switchDisplayDepthSel:
                if (TagToEnum<RenderingDepth> (item.Tag) == model.RenderingDepth)
                    item.State = NSCellStateValue.On;
                else
                    item.State = NSCellStateValue.Off;
                break;
            case toggleHiddenSel:
                item.State = model.ShowHidden ? NSCellStateValue.On : NSCellStateValue.Off;
                break;
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
                return Session.Agent.IsConnected && !model.IsVisualTreeRefreshing;
            case inspectViewSel:
                return Session.Agent.IsConnected;
            }

            return base.RespondsToSelector (sel);
        }

        [Export (toggleHiddenSel)]
        void ToggleHiddenViews (NSObject sender)
            => model.ShowHidden = ((NSMenuItem)sender).State == NSCellStateValue.On ? false : true;

        [Export (switchDisplayModeSel)]
        void SwitchDisplayMode (NSObject sender)
            => model.DisplayMode = TagToEnum<DisplayMode> (((NSMenuItem)sender).Tag);

        [Export (switchDisplayDepthSel)]
        void SwitchDisplayDepth (NSObject sender)
            => model.RenderingDepth = TagToEnum<RenderingDepth> (((NSMenuItem)sender).Tag);

        [Export (resetCameraSel)]
        void ResetCamera (NSObject sender)
        {
            ChildViewControllers.OfType<VisualRepViewController> ().ForEach (viewController => {
                viewController.ResetCamera ();
            });
        }

        [Export (refreshVisualTreeSel)]
        void RefreshVisualTree (NSObject sender)
            => model.RefreshVisualTreeAsync ().Forget ();

        [Export (inspectViewSel)]
        void InspectHighlightedView (NSObject sender)
        {
            if (!Session.Agent.IsConnected)
                return;

            var highlighter = new Highlighter (Session, model.SelectedHierarchy);
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
