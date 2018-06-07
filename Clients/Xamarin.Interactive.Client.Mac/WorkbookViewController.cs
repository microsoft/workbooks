//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Windows.Input;

using Foundation;
using ObjCRuntime;

using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Workbook.Structure;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class WorkbookViewController : SessionSplitViewController
    {
        WorkbookViewController (IntPtr handle) : base (handle)
        {
        }

        protected override void OnSessionAvailable ()
        {
            SplitViewItems [0].Collapsed = Session.SessionKind != ClientSessionKind.Workbook;

            Session.Workbook.EditorHub.Events.Subscribe (new Observer<EditorEvent> (evnt => {
                if (evnt is FocusEvent)
                    AppDelegate.SharedAppDelegate.MenuManager.Update (Session.Workbook.EditorHub);
            }));
        }

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();

            if (Session != null) {
                Session.Workbook.EditorHub.Focus ();
                AppDelegate.SharedAppDelegate.MenuManager.Update (Session.Workbook.EditorHub);
            }
        }

        public override void ViewDidDisappear ()
        {
            base.ViewDidDisappear ();

            AppDelegate.SharedAppDelegate.MenuManager.Update (null);
        }

        #region Command Selectors

        public override bool RespondsToSelector (Selector sel)
        {
            switch (sel.Name) {
            case "runAllSubmissions:":
                return Session.SessionKind != ClientSessionKind.LiveInspection && Session.CanEvaluate;
            case "addPackage:":
                return Session.CanAddPackages;
            }

            return base.RespondsToSelector (sel);
        }

        [Export ("runAllSubmissions:")]
        void RunAllSubmissions (NSObject sender)
            => Session.EvaluationService.EvaluateAllAsync ().Forget ();

        [Export ("addPackage:")]
        void AddPackage (NSObject sender)
        {
        }

        [Export ("RoutedCommand_Execute_NuGetPackageNode_Remove:parameter:")]
        void RemovePackage (NSObject sender, RoutedCommand.ParameterProxy parameter)
        {
        }

        #endregion
    }
}
