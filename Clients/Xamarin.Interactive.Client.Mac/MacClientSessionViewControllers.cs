//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using AppKit;
using Foundation;

using Xamarin.Interactive.Client.ViewControllers;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class MacClientSessionViewControllers : IClientSessionViewControllers
    {
        const string HistoryKey = "repl.history";

        public SessionWindowTabViewController WindowTabs { get; }
        public StatusToolbarViewController Status { get; }

        public History ReplHistory { get; private set; }
        public MessageViewController Messages { get; }
        public WorkbookTargetsViewController WorkbookTargets { get; }

        public MacClientSessionViewControllers (SessionWindowController sessionWindowController)
        {
            if (sessionWindowController == null)
                throw new ArgumentNullException (nameof (sessionWindowController));

            WindowTabs = sessionWindowController.TabViewController;

            Status = (StatusToolbarViewController)NSStoryboard
                .FromName ("Main", NSBundle.MainBundle)
                .InstantiateControllerWithIdentifier ("StatusToolbar");
            Status.Session = sessionWindowController.Session;

            // Set up history. Provide initial seed from NSUsrDefaults if the history file does not exist.
            string [] history = null;
            if (!History.HistoryFile.FileExists)
                history = NSUserDefaults.StandardUserDefaults.StringArrayForKey ("repl.history");

            ReplHistory = new History (history: history, persist: Prefs.Repl.SaveHistory.GetValue ());

            PreferenceStore.Default.Subscribe (ObservePreferenceChange);

            ReplHistory.Append (String.Empty);

            Messages = new MessageViewController (
                Status,
                new NSAlertMessageViewDelegate (sessionWindowController.Window));

            WorkbookTargets = new WorkbookTargetsViewController ();
        }

        void ObservePreferenceChange (PreferenceChange obj)
        {
            if (obj.Key == Prefs.Repl.SaveHistory.Key)
                ReplHistory = new History (history: ReplHistory?.Entries,
                    persist: Prefs.Repl.SaveHistory.GetValue ());
        }
    }
}