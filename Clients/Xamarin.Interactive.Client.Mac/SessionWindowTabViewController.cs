//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Foundation;
using AppKit;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
    partial class SessionWindowTabViewController
        : NSTabViewController, IObserver<ClientSessionEvent>, INSMenuValidation
    {
        readonly SessionViewControllerAdapter<SessionWindowTabViewController> sessionEventAdapter;

        public NSSegmentedControl ToolbarSegmentedControl { get; }
        public ClientSession Session => sessionEventAdapter.Session;

        WorkbookViewController workbookViewController;
        ViewInspectorMainViewController viewInspectorViewController;

        SessionWindowTabViewController (IntPtr handle) : base (handle)
        {
            sessionEventAdapter = new SessionViewControllerAdapter<SessionWindowTabViewController> (this);

            ToolbarSegmentedControl = new NSSegmentedControl {
                SegmentStyle = NSSegmentStyle.Automatic,
                TrackingMode = NSSegmentSwitchTracking.SelectOne
            };

            ToolbarSegmentedControl.Activated += ToolbarSegmentedControl_Activated;

            TransitionOptions = NSViewControllerTransitionOptions.None;
        }

        public override void ViewDidLoad ()
        {
            var storyboard = NSStoryboard.FromName ("Main", NSBundle.MainBundle);

            workbookViewController = (WorkbookViewController)storyboard
                .InstantiateControllerWithIdentifier (nameof (WorkbookViewController));

            viewInspectorViewController = (ViewInspectorMainViewController)storyboard
                .InstantiateControllerWithIdentifier (nameof (ViewInspectorMainViewController));

            AddChildViewController (workbookViewController);

            SelectedTabViewItemIndex = 0;
            ToolbarSegmentedControl.SelectedSegment = 0;

            NSWindow.Notifications.ObserveDidBecomeKey (
                (sender, e) => UpdateMainMenu (e.Notification.Object as NSWindow));

            base.ViewDidLoad ();
        }

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();
            sessionEventAdapter.ViewDidAppear ();
        }

        public EventHandler<NSTabViewItemEventArgs> ItemSelected;

        public override void DidSelect (NSTabView tabView, NSTabViewItem item)
        {
            base.DidSelect (tabView, item);
            ToolbarSegmentedControl.SelectedSegment = SelectedTabViewItemIndex;
            ItemSelected?.Invoke (this, new NSTabViewItemEventArgs (item));
        }

        void ToolbarSegmentedControl_Activated (object sender, EventArgs e)
            => SelectedTabViewItemIndex = ToolbarSegmentedControl.SelectedSegment;

        public override void AddChildViewController (NSViewController childViewController)
        {
            base.AddChildViewController (childViewController);
            UpdateUI ();
        }

        public override void InsertChildViewController (NSViewController childViewController, nint index)
        {
            base.InsertChildViewController (childViewController, index);
            UpdateUI ();
        }

        public override void RemoveChildViewController (nint index)
        {
            base.RemoveChildViewController (index);
            UpdateUI ();
        }

        public bool ValidateMenuItem (NSMenuItem item)
        {
            // Only pass through the validation recursion if the controller underneath us is a validating
            // one and actually responds to this selector, otherwise, just check RespondsToSelector.
            var viewController = TabView?.Selected?.ViewController;
            var validatingController = viewController as INSMenuValidation;
            if (validatingController != null && viewController.RespondsToSelector (item.Action))
                return validatingController.ValidateMenuItem (item);

            return RespondsToSelector (item.Action);
        }

        public override bool RespondsToSelector (ObjCRuntime.Selector sel)
        {
            var viewController = TabView?.Selected?.ViewController;
            if (viewController != null && viewController.RespondsToSelector (sel))
                return true;

            return base.RespondsToSelector (sel);
        }

        public override NSTabViewItem GetTabViewItem (NSViewController viewController)
        {
            var item = base.GetTabViewItem (viewController);
            if (item == null)
                return null;

            if (viewController is WorkbookViewController) {
                item.Image = NSImage.ImageNamed ("ToolbarConsoleTemplate");
                if (Session != null) {
                    if (Session.SessionKind == ClientSessionKind.Workbook) {
                        item.Label = Catalog.GetString ("Workbook");
                        item.ToolTip = Catalog.GetString ("Show the workbook view");
                    } else {
                        item.Label = Catalog.GetString ("REPL");
                        item.ToolTip = Catalog.GetString ("Show the REPL view");
                    }
                }
            } else if (viewController is ViewInspectorMainViewController) {
                item.Image = NSImage.ImageNamed ("ToolbarHierarchyTemplate");
                item.Label = Catalog.GetString ("View Inspector");
                item.ToolTip = Catalog.GetString ("Show the view inspector");
            }

            return item;
        }

        void UpdateUI ()
        {
            UpdateMainMenu ();
            UpdateToolbarSegmentedControl ();
        }

        void UpdateToolbarSegmentedControl ()
        {
            var childViewControllers = ChildViewControllers;

            ToolbarSegmentedControl.Hidden = childViewControllers.Length < 2;
            ToolbarSegmentedControl.SegmentCount = childViewControllers.Length;

            for (var i = 0; i < childViewControllers.Length; i++) {
                var childViewController = childViewControllers [i];
                var item = GetTabViewItem (childViewController);

                if (item.Image != null)
                    ToolbarSegmentedControl.SetImage (item.Image, i);
                else
                    ToolbarSegmentedControl.SetLabel (item.Label, i);

                if (item.ToolTip != null)
                    ToolbarSegmentedControl.Cell.SetToolTip (item.ToolTip, i);

                if (TabView?.Selected?.ViewController == childViewController)
                    ToolbarSegmentedControl.SetSelected (true, i);
            }

            ToolbarSegmentedControl.SizeToFit ();
            var controlSize = ToolbarSegmentedControl.Frame.Size;

            var toolbarItems = View?.Window?.Toolbar?.VisibleItems;
            for (var i = 0; toolbarItems != null && i < toolbarItems.Length; i++) {
                var item = toolbarItems [i];
                if (item.View == ToolbarSegmentedControl) {
                    item.MinSize = controlSize;
                    item.MaxSize = controlSize;
                }
            }
        }

        #region View Menu

        const string tabViewMenuItemSelectedSel = "tabViewMenuItemSelected:";
        static NSMenuItem [] menuItems;
        static NSWindow windowForMenuItems;

        void UpdateMainMenu (NSWindow window = null)
        {
            var viewMenuItem = NSApplication.SharedApplication.MainMenu.ItemWithTitle ("View");
            var tabViewItems = TabViewItems;
            var modifierMask = NSEventModifierMask.ShiftKeyMask | NSEventModifierMask.CommandKeyMask;

            if (window != null && window == windowForMenuItems)
                return;

            windowForMenuItems = View.Window;

            for (var i = 0; menuItems != null && i < menuItems.Length; i++)
                viewMenuItem.Submenu.RemoveItem (menuItems [i]);

            // If KeyWindow is null, update menu even if we're not sure it would be the active one.
            if ((NSApplication.SharedApplication.KeyWindow != null &&
                windowForMenuItems != NSApplication.SharedApplication.KeyWindow) ||
                tabViewItems.Length < 2) {
                menuItems = null;
                windowForMenuItems = null;
                return;
            }

            menuItems = new NSMenuItem [tabViewItems.Length];

            for (int i = tabViewItems.Length - 1; i >= 0; i--) {
                var item = menuItems [i] = new NSMenuItem (tabViewItems [i].Label) {
                    Tag = i,
                    Action = new ObjCRuntime.Selector (tabViewMenuItemSelectedSel)
                };

                // FIXME: this will need some re-thought when we introduce 3+
                // tabs. The idea is that these will navigate left/right through
                // all the available tabs, but once we have 3+, we'll need to
                // rework this logic and maybe the menu UI a little.
                if (i == tabViewItems.Length - 1) {
                    item.KeyEquivalent = "]";
                    item.KeyEquivalentModifierMask = modifierMask;
                } else if (i == 0) {
                    item.KeyEquivalent = "[";
                    item.KeyEquivalentModifierMask = modifierMask;
                }

                viewMenuItem.Submenu.InsertItem (item, 0);
            }
        }

        [Export (tabViewMenuItemSelectedSel)]
        void TabViewMenuItemSelected (NSObject sender)
        {
            var menuItem = sender as NSMenuItem;
            if (menuItem != null)
                SelectedTabViewItemIndex = menuItem.Tag;
        }

        public override nint SelectedTabViewItemIndex {
            get { return base.SelectedTabViewItemIndex; }
            set {
                base.SelectedTabViewItemIndex = value;
                UpdateUI ();
            }
        }

        #endregion

        #region IObserver<ClientSessionEvent>

        void OnAgentFeaturesUpdated ()
        {
            var supportsViewInspection = sessionEventAdapter
                .Session
                .Agent
                .Features?
                .SupportedViewInspectionHierarchies?
                .Count > 0;

            var viewInspectionTab = GetTabViewItem (viewInspectorViewController);

            if (supportsViewInspection && viewInspectionTab == null)
                AddChildViewController (viewInspectorViewController);
            else if (!supportsViewInspection && viewInspectionTab != null)
                RemoveChildViewController (TabView.IndexOf (viewInspectionTab));
        }

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
        {
            if (evnt.Kind == ClientSessionEventKind.AgentFeaturesUpdated)
                OnAgentFeaturesUpdated ();
        }

        void IObserver<ClientSessionEvent>.OnError (Exception error)
        {
        }

        void IObserver<ClientSessionEvent>.OnCompleted ()
        {
        }

        #endregion
    }
}