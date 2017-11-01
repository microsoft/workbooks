//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class SessionWindowController : NSWindowController, INSMenuValidation
    {
        const string TAG = nameof (SessionWindowController);

        public SessionWindowTabViewController TabViewController
            => (SessionWindowTabViewController)ContentViewController;

        public ClientSession Session { get; private set; }

        SessionWindowController (IntPtr handle) : base (handle)
        {
        }

        public override void WindowDidLoad ()
        {
            MainThread.Ensure ();

            Session = SessionDocument.InstanceForWindowControllers?.Session;
            if (Session == null) {
                Log.Error (TAG, "Unable to get ClientSession from SessionDocument");
                throw new InvalidOperationException ();
            }

            var viewControllers = new MacClientSessionViewControllers (this);
            Session.InitializeViewControllers (viewControllers);

            ShouldCascadeWindows = false;
            Window.FrameAutosaveName = "SessionWindow";

            Window.TitleVisibility = NSWindowTitleVisibility.Hidden;

            var toolbar = new NSToolbar ("xi-toolbar-main-toolbar-" + Guid.NewGuid ()) {
                SizeMode = NSToolbarSizeMode.Regular,
                DisplayMode = NSToolbarDisplayMode.Icon,
                AllowsUserCustomization = false
            };

            new SessionToolbarDelegate (Session, viewControllers, toolbar);

            Window.Toolbar = toolbar;

            base.WindowDidLoad ();
        }

        public bool ValidateMenuItem (NSMenuItem item)
        {
            if (TabViewController != null && TabViewController.RespondsToSelector (item.Action))
                return TabViewController.ValidateMenuItem (item);

            return RespondsToSelector (item.Action);
        }

        public override bool RespondsToSelector (ObjCRuntime.Selector sel)
        {
            if (TabViewController != null && TabViewController.RespondsToSelector (sel))
                return true;

            return base.RespondsToSelector (sel);
        }

        #region Main Menu Actions

        [Export ("zoomIn:")]
        void ZoomIn (NSObject sender)
            => Prefs.UI.Font.Update (UIFontPreference.UpdateAction.Increase);

        [Export ("zoomOut:")]
        void ZoomOut (NSObject sender)
            => Prefs.UI.Font.Update (UIFontPreference.UpdateAction.Decrease);

        [Export ("resetZoom:")]
        void ResetZoom (NSObject sender)
            => Prefs.UI.Font.Update (UIFontPreference.UpdateAction.ResetDefault);

        [Export ("terminateAgent:")]
        void TerminateAgent (NSObject sender)
        {
            #if DEBUG
			Session.TerminateAgentConnection ();
            #endif
        }

        [Export ("clearHistory:")]
        void ClearHistory (NSObject sender) => Session.ViewControllers.ReplHistory?.Clear ();

        #endregion
    }
}