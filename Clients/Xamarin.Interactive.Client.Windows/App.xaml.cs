//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client.Updater;
using Xamarin.Interactive.Client.Windows.Themes;
using Xamarin.Interactive.Client.Windows.Views;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Reflection;
using Xamarin.Interactive.SystemInformation;

namespace Xamarin.Interactive.Client.Windows
{
    partial class App : Application
    {
        const string TAG = nameof (App);

        static NewWorkbookWindow standaloneWindow;

        static FilePath appAssemblyPath = Assembly.GetExecutingAssembly ().Location;

        public static FilePath AppDirectory { get; } = new FilePath (appAssemblyPath).ParentDirectory;

        public static ImageSource AppIconSource;

        public static RecentDocumentsController RecentDocuments { get; private set; }

        [STAThread]
        public static int Main (string [] args)
        {
            if (!RemoteControl.Initialize (args))
                return 0;

            if (args.Length > 0 && args [0] == "cli") {
                var exitCode = CommandLineTool.Entry.Run (
                    args.Skip (1).ToArray (),
                    out var shouldExit);

                if (shouldExit)
                    return exitCode;

                args = Array.Empty<string> ();
            }

            var app = new App (args);
            RemoteControl.SetApp (app);
            app.InitializeComponent ();
            app.Run ();

            return 0;
        }

        readonly string [] commandLineArgsFromMain;

        App (string [] commandLineArgsFromMain)
            => this.commandLineArgsFromMain = commandLineArgsFromMain;

        protected override void OnStartup (StartupEventArgs e)
        {
            const int ieMajorRequiredVersion = 11;
            var ieMajorInstalledVersion = CrossBrowser.XcbWebView.InternetExplorerVersion.Major;
            if (ieMajorInstalledVersion < ieMajorRequiredVersion) {
                new MetroDialogWindow {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    MaxWidth = 400,
                    Title = Catalog.Format (Catalog.GetString (
                        "Unable to run {0}",
                        comment: "{0} is the application name"),
                        ClientInfo.FullProductName),
                    Message = Catalog.Format (Catalog.GetString (
                        "Internet Explorer {0} or newer is required. You have version {1}.",
                        comment: "{0} and {1} are short integers (IE major version numbers)"),
                        ieMajorRequiredVersion,
                        ieMajorInstalledVersion),
                    AffirmativeButtonText = Catalog.GetString ("Quit")
                }.ShowDialog ();
                Shutdown ();
                return;
            }

            new WindowsClientApp ().Initialize ();

            ThemeHelper.Initialize (this);

            GacCache.Initialize ();

            if (ClientInfo.Flavor == ClientFlavor.Workbooks) {
                var jumpList = new JumpList (
                    new [] {
                        new JumpTask {
                            Title = Catalog.GetString ("New Workbook"),
                            ApplicationPath = appAssemblyPath
                        }
                    },
                    showFrequent: false,
                    showRecent: false
                );

                void SyncJumpList ()
                {
                    for (var i = jumpList.JumpItems.Count - 1; i >= 0; i--) {
                        if (jumpList.JumpItems [i] is JumpTask task &&
                            !string.IsNullOrEmpty (task.Arguments))
                            jumpList.JumpItems.RemoveAt (i);
                    }

                    foreach (var document in RecentDocuments)
                        jumpList.JumpItems.Add (new JumpTask {
                            CustomCategory = Catalog.GetString ("Recent Workbooks"),
                            ApplicationPath = appAssemblyPath,
                            Arguments = ProcessControl.ProcessArguments.Quote (document.Path),
                            IconResourcePath = Path.Combine (
                                Path.GetDirectoryName (appAssemblyPath),
                                "workbook-win.ico"),
                            Title = document.Title,
                            Description = document.Path
                        });

                    JumpList.SetJumpList (this, jumpList);
                }

                RecentDocuments = new RecentDocumentsController ();
                RecentDocuments.CollectionChanged += (sender, rdcce) => SyncJumpList ();

                SyncJumpList ();
            }

            var iconPath = ClientInfo.Flavor == ClientFlavor.Inspector
                ? "pack://application:,,,/Xamarin Inspector;component/xamarin-inspector.ico"
                : "pack://application:,,,/Xamarin Workbooks;component/xamarin-workbooks-client.ico";

            var iconBitmap = new BitmapImage();
            iconBitmap.BeginInit();
            iconBitmap.UriSource = new Uri (iconPath, UriKind.Absolute);
            iconBitmap.EndInit();
            AppIconSource = iconBitmap;

            if (CommandLineTool.TestDriver.ShouldRun)
                CommandLineTool.TestDriver.Run (Current.Dispatcher.Invoke);
            else
                HandleCommandLineArgs (commandLineArgsFromMain, fromStartup: true);

            base.OnStartup (e);

            ClientApp.SharedInstance.Updater.CheckForUpdatesPeriodicallyInBackground (
                update => UpdateHandler (null, update));
        }

        public static void CheckForUpdatesInBackground (Window ownerWindow = null, bool userInitiated = false)
            => ClientApp.SharedInstance.Updater.CheckForUpdatesInBackground (
                userInitiated, update => UpdateHandler (ownerWindow, update));

        static UpdaterWindow updaterWindow;

        static void UpdaterWindow_Closed (object sender, EventArgs e)
        {
            var window = (UpdaterWindow)sender;
            window.Closed -= UpdaterWindow_Closed;
            if (updaterWindow == window)
                updaterWindow = null;
        }

        static void UpdateHandler (Window ownerWindow, Task<UpdateItem> update)
        {
            if (update.IsFaulted) {
                if (ownerWindow != null)
                    update.Exception.ToUserPresentable (
                        Catalog.GetString ("An error occurred when checking for updates."))
                            .Present (ownerWindow);
                return;
            }

            if (update.IsCanceled)
                return;

            if (update.Result != null) {
                if (updaterWindow != null) {
                    // the user is actively doing something with the previous window, or
                    // an update came in that is the same as what's already up on screen,
                    // so just let the existing window handle it.
                    if (updaterWindow.IsWorking ||
                        updaterWindow.UpdateReleaseVersion == update.Result.ReleaseVersion)
                        return;

                    updaterWindow.Close ();
                }

                updaterWindow = new UpdaterWindow (update.Result);
                updaterWindow.Closed += UpdaterWindow_Closed;
                updaterWindow.Show ();
                return;
            }

            if (ownerWindow != null)
                new MetroDialogWindow {
                    Owner = ownerWindow,
                    Width = ownerWindow.Width,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Title = Catalog.Format (Catalog.GetString (
                        "{0} is up to date!",
                        comment: "{0} is the application name"),
                        ClientInfo.FullProductName),
                    Message = Catalog.Format (Catalog.GetString (
                        "{0} is the latest version available in the Xamarin " +
                        "{1} channel, released on {2}.",
                        comment: "{0} is a version number; {1} is a channel; " +
                            "{2} is a localized date"),
                        BuildInfo.Version.ToString (Versioning.ReleaseVersionFormat.FriendlyShort),
                        ClientApp.SharedInstance.Updater.UpdateChannel,
                        BuildInfo.Date.ToString ("D")),
                    AffirmativeButtonText = Catalog.GetString ("OK")
                }.ShowDialog ();
        }

        public bool CanShutdown => Windows.OfType<AgentSessionWindow> ().All (w => !w.IsDirty);

        public static void CheckNeedsExit ()
        {
            if (!Current.Windows.Cast<Window> ().Any (w => w.IsVisible))
                Current.Shutdown ();
        }

        public static void ShowStandaloneWindow (AgentType? agentType = null)
        {
            if (standaloneWindow == null)
                standaloneWindow = new NewWorkbookWindow ();
            if (agentType.HasValue)
                standaloneWindow.SelectedAgentType = agentType.Value;
            standaloneWindow.Show ();
            standaloneWindow.Activate ();
        }

        public static void OpenWorkbook ()
        {
            var openDialog = new CommonOpenFileDialog {
                Title = Catalog.GetString ("Open Workbook"),
                DefaultExtension = ".workbook"
            };

            openDialog.Filters.Add (new CommonFileDialogFilter ("Xamarin Workbook", ".workbook"));

            if (openDialog.ShowDialog () != CommonFileDialogResult.Ok)
                return;

            OpenWorkbook (new Uri (openDialog.FileName));
        }

        public static AgentSessionWindow OpenWorkbook (Uri uri)
        {
            try {
                return AgentSessionWindow.Open (new ClientSessionUri (uri));
            } catch (Exception e) {
                Log.Error (nameof (OpenWorkbook), e);
                MessageBox.Show ("Please see the log for details.", "Workbook failed to load");
                return null;
            }
        }

        static void OpenWorkbookFromCommandLine (Uri uri)
        {
            var success = false;
            try {
                success |= OpenWorkbook (uri) != null;
            } catch (Exception e) {
                Log.Error (nameof (OpenWorkbookFromCommandLine), e);
            }

            if (!success)
                CheckNeedsExit ();
        }

        static void HandlePostInstall (string installProductCode)
        {
            try {
                // Overwrite the boring System.Version with our lovely SemVer.
                using (var arpKey = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey ($"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{Guid.Parse (installProductCode):B}\\",
                        writable: true)) {
                    arpKey?.SetValue ("DisplayVersion", BuildInfo.VersionString);
                }
            } catch {
                // Throwing here would roll back the entire install.
                // Not worth logging unless we start adding more heavy lifting here.
            }
        }

        void HandleCommandLineArgs (string[] args, bool fromStartup = false)
        {
            var badArgs = false;

            if (args.Length > 1 && args [0] == "--postinstall") {
                if (args.Length != 2)
                    throw new Exception ("Invalid arguments for --postinstall.");
                HandlePostInstall (args [1]);
                Shutdown ();
                return;
            } else if (args.Length == 1) {
                var arg = args [0];
                ClientSessionUri agentConnectUri = null;

                if (arg == "--shutdown") {
                    Shutdown ();
                    return;
                } else if (Path.GetExtension (arg) == ".workbook") {
                    OpenWorkbookFromCommandLine (new Uri (arg));
                    return;
                } else {
                    try {
                        Log.Info (TAG, $"Launched with URI: {arg}");
                        agentConnectUri = new ClientSessionUri (new Uri (arg));
                    } catch {
                        badArgs = true;
                    }
                }

                if (agentConnectUri != null)
                    AgentSessionWindow.Open (agentConnectUri);
            } else if (ClientInfo.Flavor == ClientFlavor.Workbooks && args.Length == 0) {
                ShowStandaloneWindow ();
            } else
                badArgs = true;

            // Ignore bad args from secondary instances
            if (badArgs && fromStartup)
                Shutdown ();
        }

        class RemoteControl : MarshalByRefObject
        {
            static Mutex mutex;
            static bool firstInstance;
            static readonly string MutexName = ClientInfo.Flavor == ClientFlavor.Inspector
                ? "{97E133BD-F7C0-4F45-9629-0023D04EF4E3}"
                : "{4CB23FE3-3556-46CE-86AA-61E16E868E34}";

            static IpcChannel ipcChannel;
            static readonly string ServerName = $"Xamarin{ClientInfo.ShortProductName}WindowsClientServer";
            static readonly string ClientName = $"Xamarin{ClientInfo.ShortProductName}WindowsClientClient";
            static readonly string ServiceName = $"Xamarin{ClientInfo.ShortProductName}WindowsRemoteControl";
            static readonly string ServiceUrl = $"ipc://{ServerName}/{ServiceName}";

            static App app;

            public static bool Initialize (string [] args)
            {
                if (CheckFirstInstance ()) {
                    ipcChannel = new IpcChannel (ServerName);
                    ChannelServices.RegisterChannel (ipcChannel, false);
                    RemotingConfiguration.RegisterWellKnownServiceType (
                        typeof (RemoteControl),
                        ServiceName,
                        WellKnownObjectMode.Singleton);

                    Activator.GetObject (typeof (RemoteControl), ServiceUrl);

                    return true;
                }

                // Not first instance
                ipcChannel = new IpcChannel (ClientName);
                ChannelServices.RegisterChannel (ipcChannel, false);

                var remote = (RemoteControl)Activator.GetObject (typeof (RemoteControl), ServiceUrl);
                remote.SignalFirstInstance (args);

                return false;
            }

            public void SignalFirstInstance (IList<string> args)
            {
                app.Dispatcher.BeginInvoke (
                    new DispatcherOperationCallback (SignalFirstInstanceCallback),
                    args);
            }

            public static void SetApp (App app)
            {
                RemoteControl.app = app;
            }

            static bool CheckFirstInstance ()
            {
                if (mutex == null)
                    mutex = new Mutex (true, MutexName, out firstInstance);
                return firstInstance;
            }

            static object SignalFirstInstanceCallback (object args)
            {
                app.HandleCommandLineArgs ((string[]) args);
                return null;
            }

            public override object InitializeLifetimeService ()
            {
                return null;
            }
        }
    }
}
