//
// AppDelegate.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AppKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Client.Mac.Menu;
using Xamarin.Interactive.Client.Mac.Roslyn;
using Xamarin.Interactive.Client.Updater;

namespace Xamarin.Interactive.Client.Mac
{
    [Register ("AppDelegate")]
    class AppDelegate : NSApplicationDelegate
    {
        public static readonly NSError SuppressionNSError = new NSError (
            NSError.CocoaErrorDomain,
            3072, // NSUserCancelledError */
            null);

        public static AppDelegate SharedAppDelegate => (AppDelegate)NSApplication.SharedApplication.Delegate;

        public const string ErrorDomain = "XamarinInspectorDomain";

        const string TAG = nameof (AppDelegate);

        const string getUrlWithReplyEventSel = "getUrl:withReplyEvent:";

        public MenuManager MenuManager { get; private set; }

        bool applicationShouldOpenUntitledFile = true;

        public AppDelegate ()
        {
            new MacClientApp ().Initialize ();

            Log.EntryAdded += LogEntryAdded;

            var workbookAppsDirectory = new FilePath (NSBundle.MainBundle.SharedSupportPath)
                .Combine ("WorkbookApps");
            if (workbookAppsDirectory.DirectoryExists)
                WorkbookAppInstallation.RegisterSearchPath (workbookAppsDirectory);

            var macosVersionFull = NSProcessInfo.ProcessInfo.OperatingSystemVersion;
            var macosVersion = new Version (
                (int)macosVersionFull.Major,
                (int)macosVersionFull.Minor,
                (int)macosVersionFull.PatchVersion);
            WorkbookAppInstallation.RegisterPathMapper (p => {
                const string macosVersionVar = "{macosVersion}";
                var index = p.IndexOf (macosVersionVar, StringComparison.Ordinal);
                if (index < 0)
                    return p;

                var versionsParentDir = p.Substring (0, index);
                if (!Directory.Exists (versionsParentDir))
                    return p;

                var highestCompatibleVersionDir = Directory
                    .EnumerateDirectories (versionsParentDir)
                    .Select (d => Path.GetFileName (d))
                    .Select (vd => new {
                        DirectoryName = vd,
                        Version = Version.TryParse (vd, out var version) ? version : null,
                    })
                    .Where (vp => vp.Version != null)
                    .OrderByDescending (vp => vp.Version)
                    .FirstOrDefault (vp => vp.Version <= macosVersion);
                if (highestCompatibleVersionDir == null)
                    return p;

                return p.Replace (macosVersionVar, highestCompatibleVersionDir.DirectoryName);
            });

            NSAppleEventManager.SharedAppleEventManager.SetEventHandler (this,
                new ObjCRuntime.Selector (getUrlWithReplyEventSel),
                AEEventClass.Internet, AEEventID.GetUrl);

            NSWindowExtensions.DisableAutomaticWindowTabbing ();
        }

        void LogEntryAdded (object provider, LogEntry entry)
            => logWindowController.AppendLogEntry (entry);

        [Export (getUrlWithReplyEventSel)]
        void GetUrlWithReplyEvent (NSAppleEventDescriptor @event, NSAppleEventDescriptor replyEvent)
        {
            // from /System/Library/Frameworks/CoreServices.framework/Versions/A/Frameworks/AE.framework/Versions/A/Headers/AppleEvents.h
            const uint keyDirectObject = 757935405; // '----'
            const uint keyErrorNumber = 1701999214; // 'errn'
            const uint keyErrorString = 1701999219; // 'errs'

            string errorMessage = null;

            var urlString = @event?.ParamDescriptorForKeyword (keyDirectObject).StringValue;
            if (urlString != null) {
                SessionDocumentController
                    .SharedDocumentController
                    .OpenDocument (new NSUrl (urlString));

                applicationShouldOpenUntitledFile = false;
            }

            replyEvent.SetParamDescriptorforKeyword (
                NSAppleEventDescriptor.DescriptorWithInt32 (errorMessage == null ? 0 : 1),
                keyErrorNumber);

            if (errorMessage != null)
                replyEvent.SetParamDescriptorforKeyword (
                    NSAppleEventDescriptor.DescriptorWithString (errorMessage),
                    keyErrorString);
        }

        public override bool ApplicationShouldOpenUntitledFile (NSApplication sender)
            => applicationShouldOpenUntitledFile;

        public override void WillFinishLaunching (NSNotification notification)
        {
            MenuManager = new MenuManager (NSApplication.SharedApplication.MainMenu);

            ClientApp.SharedInstance.Updater.CheckForUpdatesPeriodicallyInBackground (
                update => UpdateHandler (null, update));

            var helpMenu = NSApplication.SharedApplication.HelpMenu;

            if (ClientInfo.Flavor == ClientFlavor.Inspector) {
                var mainMenuItems = NSApplication.SharedApplication.MainMenu.ItemArray ();

                // Update menu items in app/help menus
                var menuItems = mainMenuItems [0].Submenu.ItemArray ().Concat (helpMenu.ItemArray ());
                foreach (var item in menuItems)
                    item.Title = item.Title.Replace ("Workbooks", "Inspector");

                applicationShouldOpenUntitledFile = false;
            } else {
                var sampleWorkbooksMenuItem = new NSMenuItem (Catalog.GetString ("Tutorials"));
                sampleWorkbooksMenuItem.Submenu = new NSMenu ();
                helpMenu.InsertItem (sampleWorkbooksMenuItem, 0);

                var workbookFiles = new FilePath (NSBundle.MainBundle.ResourcePath)
                    .Combine ("Workbooks")
                    .EnumerateFiles ("*.workbook", SearchOption.TopDirectoryOnly);

                foreach (var workbookFile in workbookFiles)
                    sampleWorkbooksMenuItem.Submenu.AddItem (new NSMenuItem (
                        workbookFile.NameWithoutExtension,
                        (sender, e) => NSWorkspace.SharedWorkspace.OpenFile (workbookFile)));

                sampleWorkbooksMenuItem.Submenu.AddItem (NSMenuItem.SeparatorItem);
                sampleWorkbooksMenuItem.Submenu.AddItem (new NSMenuItem (
                    ClientInfo.DownloadWorkbooksMenuLabel,
                    (sender, e) => NSWorkspace.SharedWorkspace.OpenUrl (
                        ClientInfo.DownloadWorkbooksUri)));
            }

            if (CommandLineTool.TestDriver.ShouldRun)
                CommandLineTool.TestDriver.Run (NSApplication.SharedApplication.InvokeOnMainThread);

            new SessionDocumentController ();
        }

        #region Main Menu Actions

        [Export ("installCommandLineTool:")]
        public void InstallCommandLineTool (NSObject sender)
            => InstallCommandLineToolAsync ().Forget ();

        static async Task InstallCommandLineToolAsync ()
        {
            try {
                const string scriptPath = "/usr/local/bin/workbook";
                var authOpen = await Security.AuthOpen.PreauthorizeAsync (
                    new XamCore.Security.AuthorizationEnvironment {
                        Prompt = Catalog.Format (Catalog.GetString (
                            "{0} would like to install a script to " +
                            "support terminal usage: {1}",
                            comment: "{0} is the app name; {1} is a file path"),
                            ClientInfo.FullProductName,
                            scriptPath),
                        AddToSharedCredentialPool = true
                    },
                    scriptPath,
                    mode: 493 /*0755*/);

                await authOpen.WriteAsync (h => h.WriteData (NSData.FromString (
                    "#!/bin/bash\n" +
                    "\"" + NSBundle.MainBundle.ExecutablePath + "\" cli \"$@\""
                )));

                new NSAlert {
                    AlertStyle = NSAlertStyle.Informational,
                    MessageText = Catalog.GetString ("Installation Complete"),
                    InformativeText = Catalog.Format (Catalog.GetString (
                        "The '{0}' tool has been installed to {1}. " +
                        "Ensure that {1} is in your PATH.",
                        comment: "{0} and {1} are both file paths"),
                        Path.GetFileName (scriptPath),
                        Path.GetDirectoryName (scriptPath))
                }.RunModal ();
            } catch (Exception e) {
                e.ToUserPresentable (
                    Catalog.GetString ("Unable to install command line tool")
                ).Present ();
            }
        }

        public override bool OpenFile (NSApplication sender, string filename)
        {
            SessionDocumentController
                .SharedDocumentController
                .OpenDocument (new NSUrl (filename));
            return true;
        }

        [Export ("showOnlineHelp:")]
        public void ShowHelp (NSObject sender)
            => NSWorkspace.SharedWorkspace.OpenUrl (ClientInfo.HelpUri);

        [Export ("showForums:")]
        public void ShowForums (NSObject sender)
            => NSWorkspace.SharedWorkspace.OpenUrl (ClientInfo.ForumsUri);

        [Export ("copyVersionInformation:")]
        public void CopyVersionInformation (NSObject sender)
        {
            var pasteboard = NSPasteboard.GeneralPasteboard;
            pasteboard.ClearContents ();
            pasteboard.SetStringForType (
                ClientApp.SharedInstance.GetIssueReportForClipboard (),
                NSPasteboard.NSStringType);
        }

        [Export ("showPrivacyStatement:")]
        public void ShowPrivacyStatement (NSObject sender)
            => NSWorkspace.SharedWorkspace.OpenUrl (ClientInfo.MicrosoftPrivacyStatementUri);

        // FIXME: lazily load this controller... currently it's created
        // immediately to actually capture log entries for display.
        readonly LogWindowController logWindowController = new LogWindowController ();

        [Export ("showLogWindow:")]
        void ShowLogWindow (NSObject sender)
        {
            logWindowController.Window.MakeKeyAndOrderFront (sender);
        }

        AboutWindowController aboutWindowController;

        [Export ("showAbout:")]
        void ShowAbout (NSObject sender)
        {
            if (aboutWindowController == null)
                aboutWindowController = new AboutWindowController ();
            aboutWindowController.Window.MakeKeyAndOrderFront (sender);
        }

        PreferencesWindowController preferencesWindowController;

        [Export ("showPreferences:")]
        void ShowPreferences (NSObject sender)
        {
            if (preferencesWindowController == null)
                preferencesWindowController = NSStoryboard
                    .FromName ("Main", NSBundle.MainBundle)
                    .InstantiateController<PreferencesWindowController> ();

            preferencesWindowController.Window.MakeKeyAndOrderFront (sender);
        }

        NSWindowController updaterWindowController;
        NSStoryboard updaterStoryboard = NSStoryboard.FromName ("Updater", NSBundle.MainBundle);

        [Export ("checkForUpdates:")]
        void CheckForUpdates (NSObject sender)
        {
            if (sender == null)
                throw new ArgumentNullException (nameof (sender));

            if (updaterWindowController != null) {
                updaterWindowController.Window.MakeKeyAndOrderFront (sender);
                return;
            }

            updaterWindowController = (NSWindowController)updaterStoryboard
                .InstantiateControllerWithIdentifier ("CheckingForUpdatesWindowController");
            updaterWindowController.Window.Center ();

            CancellationTokenSource cancellation = null;

            updaterWindowController.Window.WindowShouldClose = (o) => {
                cancellation?.Cancel ();
                return true;
            };

            updaterWindowController.Window.MakeKeyAndOrderFront (sender);

            cancellation = ClientApp
                .SharedInstance
                .Updater
                .CheckForUpdatesInBackground (
                    true,
                    update => UpdateHandler (sender, update));
        }

        void UpdateHandler (NSObject sender, Task<UpdateItem> update)
        {
            if (updaterWindowController != null) {
                updaterWindowController.Close ();
                updaterWindowController.Dispose ();
                updaterWindowController = null;
            }

            if (update.IsFaulted) {
                if (sender != null)
                    MacUpdaterViewModel.PresentUpdateCheckFailedDialog (update.Exception);
                return;
            }

            if (update.IsCanceled)
                return;

            if (update.Result == null) {
                if (sender != null)
                    MacUpdaterViewModel.PresentUpToDateDialog ();
                return;
            }

            updaterWindowController = (NSWindowController)updaterStoryboard
                .InstantiateControllerWithIdentifier ("UpdateAvailableWindowController");

            updaterWindowController.Window.WillClose += (o, e) => {
                updaterWindowController.Dispose ();
                updaterWindowController = null;
            };

            ((UpdaterViewController)updaterWindowController.ContentViewController)
                .PresentUpdate (update.Result);
        }

        NSWindowController connectToAgentWindowController;

        [Export ("connectToAgent:")]
        void OpenLocation (NSObject sender)
        {
            if (connectToAgentWindowController == null)
                connectToAgentWindowController = (NSWindowController)NSStoryboard
                    .FromName ("Main", NSBundle.MainBundle)
                    .InstantiateControllerWithIdentifier ("ConnectToAgentWindowController");

            connectToAgentWindowController.Window.MakeKeyAndOrderFront (sender);
        }

        RoslynWorkspaceExplorerWindowController roslynWorkspaceExplorerWindowController;

        [Export ("showRoslynWorkspaceExplorer:")]
        void ShowRoslynWorkspaceExplorer (NSObject sender)
        {
            if (roslynWorkspaceExplorerWindowController == null)
                roslynWorkspaceExplorerWindowController = new RoslynWorkspaceExplorerWindowController ();
            roslynWorkspaceExplorerWindowController.Window.MakeKeyAndOrderFront (sender);
        }

        #endregion
    }
}
