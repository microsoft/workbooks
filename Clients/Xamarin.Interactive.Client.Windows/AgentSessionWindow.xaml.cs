//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Events;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.NuGet;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.PropertyEditor;
using Xamarin.Interactive.Workbook.LoadAndSave;
using Xamarin.Interactive.Workbook.Structure;
using Xamarin.Interactive.Workbook.Views;

using Xamarin.Interactive.Client.Windows.ViewModels;
using Xamarin.Interactive.Client.Windows.Views;

using XIR = Xamarin.Interactive.Remote;
using Xamarin.PropertyEditing.Themes;
using Xamarin.PropertyEditing.Windows;
using Xamarin.Interactive.Client.PropertyEditor;

namespace Xamarin.Interactive.Client.Windows
{
    partial class AgentSessionWindow : MetroWindow, INotifyPropertyChanged, IObserver<ClientSessionEvent>
    {
        const string TAG = nameof (AgentSessionWindow);

        readonly MenuManager menuManager;
        NativeWebBrowserEventHandler browserEventHandler;
        IDisposable preferenceSubscription;
        XcbWebView webView;

        public ClientSession Session { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged ([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        public bool IsDirty { get; private set; }

        public WpfMessageViewDelegate MessageViewDelegate { get; }

        public WpfDialogMessageViewDelegate DialogMessageViewDelegate { get; }

        public static ClientSessionController<AgentSessionWindow> SessionController
            = new ClientSessionController<AgentSessionWindow> ();

        public ViewInspectorViewModel<AgentSessionWindow> ViewModel { get; }

        public static AgentSessionWindow Open (ClientSessionUri clientSessionUri)
        {
            AgentSessionWindow window;

            if (!SessionController.TryGetApplicationState (clientSessionUri, out window))
                window = new AgentSessionWindow (clientSessionUri);

            window.Show ();
            window.Activate ();

            // Sometimes (like when clicking the Inspect button in VS), Activate fails to
            // bring the client window to the foreground (very obvious if it is behind VS).
            // This hack temporarily sets the window to be always-on-top to force it to
            // the foreground, then immediately unsets that property.
            window.Topmost = true;
            window.Topmost = false;

            return window;
        }

        enum SaveOperation
        {
            Save,
            SaveAs
        }

        bool Save (SaveOperation operation)
        {
            if (!CanSave)
                return false;

            var saveOperation = Session.CreateWorkbookSaveOperation ();
            var savePath = Session.Workbook.LogicalPath;

            if (savePath.IsNull || operation == SaveOperation.SaveAs) {
                var saveDialog = new CommonSaveFileDialog {
                    Title = Catalog.GetString ("Save Workbook"),
                    DefaultFileName = Session.Title,
                    AlwaysAppendDefaultExtension = true,
                    DefaultExtension = ".workbook"
                };

                if (!savePath.IsNull)
                    saveDialog.InitialDirectory = savePath.DirectoryExists
                        ? savePath.ParentDirectory
                        : savePath;

                saveDialog.FolderChanging += (o, e) => {
                    if (e.Folder.EndsWith (".workbook", StringComparison.OrdinalIgnoreCase))
                        e.Cancel = true;
                };

                saveDialog.Filters.Add (new CommonFileDialogFilter ("Xamarin Workbook", ".workbook"));

                CommonFileDialogComboBox formatComboBox = null;

                if (saveOperation.SupportedOptions.HasFlag (WorkbookSaveOptions.Archive)) {
                    formatComboBox = new CommonFileDialogComboBox ();
                    formatComboBox.Items.Add (new CommonFileDialogComboBoxItem (
                        Catalog.GetString ("Package Directory")));
                    formatComboBox.Items.Add (new CommonFileDialogComboBoxItem (
                        Catalog.GetString ("Archive")));
                    formatComboBox.SelectedIndex = saveOperation.Options.HasFlag (
                        WorkbookSaveOptions.Archive) ? 1 : 0;

                    var formatGroup = new CommonFileDialogGroupBox ("Workbook Format:");
                    formatGroup.Items.Add (formatComboBox);
                    saveDialog.Controls.Add (formatGroup);
                }

                if (saveDialog.ShowDialog (this) != CommonFileDialogResult.Ok)
                    return false;

                if (formatComboBox != null)
                    switch (formatComboBox.SelectedIndex) {
                    case 0:
                        saveOperation.Options &= ~WorkbookSaveOptions.Archive;
                        break;
                    case 1:
                        saveOperation.Options |= WorkbookSaveOptions.Archive;
                        break;
                    default:
                        throw new IndexOutOfRangeException ();
                    }

                savePath = saveDialog.FileName;

                saveOperation.Destination = savePath;
            }

            var success = false;
            try {
                Session.SaveWorkbook (saveOperation);
                NoteRecentDocument ();
                success = true;
                IsDirty = false;
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return success;
        }

        public bool CanSave => Session.SessionKind == ClientSessionKind.Workbook;

        AgentSessionWindow (ClientSessionUri clientSessionUri)
        {
            MessageViewDelegate = new WpfMessageViewDelegate (this);
            DialogMessageViewDelegate = new WpfDialogMessageViewDelegate (this);

            Session = new ClientSession (clientSessionUri);
            Session.InitializeViewControllers (
                new WpfClientSessionViewControllers (MessageViewDelegate, DialogMessageViewDelegate));

            SessionController.AddSession (Session, this);

            InitializeComponent ();
            DataContext = this;
            ViewModel = new ViewInspectorViewModel<AgentSessionWindow> (Session, this);
            menuManager = new MenuManager (mainMenu, this, Session.SessionKind != ClientSessionKind.LiveInspection);

            replWebView.Loaded += HandleWebViewControlLoaded;
            replWebView.LoadCompleted += HandleWebViewSourceLoadCompleted;

            propertyEditor.EditorProvider = new InteractiveEditorProvider (Session, new CommonPropertyViewHelper ());
            PropertyEditorPanel.ThemeManager.Theme = PropertyEditorTheme.Light;
        }

        void NoteRecentDocument ()
        {
            if (Session.Workbook.LogicalPath.Exists)
                App.RecentDocuments?.Add (new RecentDocument (
                    Session.Workbook.LogicalPath,
                    Session.Workbook.Title
                ));
        }

        protected override void OnClosing (CancelEventArgs e)
        {
            if (IsDirty && CanSave) {
                var dlg = new MetroDialogWindow {
                    Owner = this,
                    Title = "Unsaved Changes",
                    Width = Width,
                    Message = $"Do you want to save changes to {Session.Title}?",
                    ButtonStyle = MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary,
                    AffirmativeButtonText = "save",
                    NegativeButtonText = "don't save",
                };
                dlg.ShowDialog ();

                if (dlg.Result == MessageDialogResult.Affirmative) {
                    if (!Save (SaveOperation.Save)) {
                        e.Cancel = true;
                        return;
                    }
                } else if (dlg.Result == MessageDialogResult.FirstAuxiliary) {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing (e);
        }

        protected override void OnClosed (EventArgs e)
        {
            base.OnClosed (e);

            preferenceSubscription?.Dispose ();
            ViewModel?.Dispose ();

            if (Session != null) {
                Session.Dispose ();
                SessionController.RemoveSession (Session);
            }

            App.CheckNeedsExit ();
        }

        /// <summary>
        /// This is the WPF standard control Loaded handler. It can be raised
        /// multiple times, so disconnect it on our first invocation. Once raised
        /// it is safe to connect to the session.
        /// </summary>
        void HandleWebViewControlLoaded (object sender, RoutedEventArgs e)
        {
            replWebView.Loaded -= HandleWebViewControlLoaded;
            Session.Subscribe (this);
        }

        bool HandleNavigation (Uri uri)
        {
            var resourceAction = ClientApp
                .SharedInstance
                .WebServer
                .TryGetLocalResourcePath (
                    uri,
                    out var localPath);

            Uri launchUri = null;

            switch (resourceAction) {
            case ClientWebServer.ResourceAction.WorkbookResource:
                launchUri = new Uri (localPath.FullPath);
                break;
            case ClientWebServer.ResourceAction.ExternalResource:
                launchUri = uri;
                break;
            }

            if (launchUri != null)
                Process.Start (launchUri.OriginalString);
            else
                ScrollToElementWithId (uri.Fragment.TrimStart ('#'));

            return true;
        }

        bool ScrollToElementWithId (string fragment)
        {
            if (!string.IsNullOrEmpty (fragment)) {
                ((XcbWorkbookPageView)Session.WorkbookPageViewModel).ScrollToElementWithId (fragment);
                return true;
            }

            return false;
        }

        /// <summary>
        /// This is our indication that we've loaded the ClientWebServer workbook
        /// "web app" and can now play around with the DOM, scripting, etc, and
        /// should kick off the client connection to the agent.
        /// </summary>
        void HandleWebViewSourceLoadCompleted (object sender, NavigationEventArgs navigationArgs)
        {
            // should never get more than one source load (because we only ever set it once,
            // but this is to keep sanity since the entire agent/client init process kicks
            // off here. It's not good to happen multiple times ;)
            replWebView.LoadCompleted -= HandleWebViewSourceLoadCompleted;

            replWebView.Navigating += (s, args) => args.Cancel = HandleNavigation (args.Uri);

            // When alt-tabbing away and back, WebBrowser focus gets lost. So we just always restore focus
            // to the browser control when this window is activated.
            Activated += (o, e) => webView.Focus ();

            // since most of the REPL is monospace and the major focus of the UI, increasing
            // the default font size by two points helps a lot for legibility and rendering
            // of the font itself.
            Prefs.UI.Font.DefaultFontSize = FontSize + 2;
            Prefs.UI.Font.MinFontSize = Prefs.UI.Font.DefaultFontSize / 2;

            var styleElement = (HtmlStyleElement)webView.Document.CreateElement ("style");
            webView.Document.Head.AppendChild (styleElement);
            styleElement.Sheet.InsertRule ($"body {{ font-family: '{FontFamily}' !important; }}", 0);

            // hack because we can't set a preferred/default 'monospace' font family globally
            // on IE like we can with WebKit, so we must override everything that explicitly
            // uses monospace for now :(
            styleElement.Sheet.InsertRule (@"
                pre,
                code,
                .interactive-workspace > article.submission > section > .diagnostics ul,
                .exception .stack-frame,
                .renderer-base .to-string-representation,
                .renderer-captured-output,
                .renderer-enumerable > header::before,
                .renderer-enumerable > header.expanded::before,
                .renderer-enumerable > ol > li::before,
                .renderer-help td:first-child,
                .renderer-object > table > tbody > tr > th,
                .renderer-object > table > tbody > tr > td,
                .CodeMirror,
                .CodeMirror-hints,
                .CodeMirror-dialog input {
                    font-family: 'Consolas', 'Lucida Console', monospace !important;
                }
            ", 0);

            browserEventHandler = new NativeWebBrowserEventHandler (replWebView);
            browserEventHandler.BeforeNavigate += (o, args) => {
                // Cancel everything but javascript URLs and about:blank. At this point, the event handler
                // is only getting called for iframe navigation events. We need to allow
                // "javascript:" to let our own srcdoc load succeed. After that the sandbox
                // is enabled and we don't have to worry about malicious javascript.
                //
                // about:blank is required for initial setup of the iframe.
                if (!args.Url.StartsWith ("javascript:") && !args.Url.StartsWith ("about:blank")) {
                    args.Cancel = true;
                    Process.Start (args.Url);
                }
            };
        }

        /// <summary>
        /// The ActiveX control that WebBrowser uses has complex
        /// focus rules, we work around them by focusing the pane group
        /// when toggling back to the workbook/repl tab then the WebBroser.Focus ()
        /// logic correctly handles the navigation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void HandleSelectedPaneChanged (object sender, EventArgs args)
        {
            var group = sender as Selector;

            if (group != null) {
                switch (group.SelectedIndex) {
                case 0:
                    group.Focus ();
                    webView?.Focus ();
                    menuManager.RemoveMenu ((MenuItem)Resources ["viewMenu"]);
                    break;
                case 1:
                    group.Focus ();
                    if (ViewModel.SelectedView == null)
                        ViewModel.RefreshVisualTreeAsync ().Forget ();

                    var menuItem = (MenuItem)Resources ["viewMenu"];
                    if (menuItem.Parent == null)
                        menuManager.AddMenu (menuItem);
                    break;
                }
            }
        }

        void ObservePreferenceChange (PreferenceChange change)
        {
            if (change.Key == Prefs.UI.Font.Key)
                UpdateFont ();
        }

        void UpdateFont ()
        {
            var fontSize = Prefs.UI.Font.GetSize ();
            webView.Document.DocumentElement.Style.SetProperty ("font-size", $"{fontSize}px");
        }

        async void InspectModel_PropertyChanged (object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName) {
                case "SelectedView":
                    await SetSelectedViewAsync (ViewModel.SelectedView);
                    break;
            }
        }

        public async Task SetSelectedViewAsync (XIR.InspectView view)
        {
            if (!Session.Agent.IsConnected)
                return;

            if (string.IsNullOrEmpty (view?.PublicType))
                return;

            var remoteProperty = await Session.Agent.Api.GetObjectMembersAsync (view.Handle);

            propertyEditor.SelectedItems.Clear ();
            propertyEditor.SelectedItems.Add (remoteProperty);

            if (!string.IsNullOrEmpty (view?.PublicCSharpType)
                && Session.SessionKind == ClientSessionKind.LiveInspection)
                await Session.WorkbookPageViewModel.EvaluateAsync (
                    $"var selectedView = GetObject<{view.PublicCSharpType}> (0x{view.Handle:x})");
        }

        void OnClearHistory (object sender, ExecutedRoutedEventArgs e)
            => Session.ViewControllers.ReplHistory?.Clear ();

        void OnIncreaseFont (object sender, ExecutedRoutedEventArgs e)
            => Prefs.UI.Font.Update (UIFontPreference.UpdateAction.Increase);

        void OnDecreaseFont (object sender, ExecutedRoutedEventArgs e)
            => Prefs.UI.Font.Update (UIFontPreference.UpdateAction.Decrease);

        void OnResetFontSize (object sender, ExecutedRoutedEventArgs e)
            => Prefs.UI.Font.Update (UIFontPreference.UpdateAction.ResetDefault);

        void ReplWebView_OnPreviewKeyDown (object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5) {
                e.Handled = true;
                return;
            }

            // Prevent IE shortcuts like Ctrl+N/O. Replace IE zooming with our own.
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                return;

            switch (e.Key) {
            case Key.N:
                e.Handled = true;
                ApplicationCommands.New.Execute (null, this);
                break;
            case Key.O:
                e.Handled = true;
                ApplicationCommands.Open.Execute (null, this);
                break;
            case Key.S:
                e.Handled = true;
                ApplicationCommands.Save.Execute (null, this);
                break;
            case Key.G:
                e.Handled = true;
                NuGetPackagesNode.AddPackage.Execute (null, this);
                break;
            case Key.OemPlus:
                e.Handled = true;
                Commands.Commands.IncreaseFont.Execute (null, this);
                break;
            case Key.OemMinus:
                e.Handled = true;
                Commands.Commands.DecreaseFont.Execute (null, this);
                break;
            case Key.D0:
            case Key.NumPad0:
                e.Handled = true;
                Commands.Commands.ResetFontSize.Execute (null, this);
                break;
            case Key.P:
                e.Handled = true;
                break;
            }
        }

        static T FindFirstVisualChild<T> (DependencyObject obj) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount (obj); i++) {
                var child = VisualTreeHelper.GetChild (obj, i);
                var tChild = child as T;
                if (tChild != null)
                    return tChild;

                var grandChild = FindFirstVisualChild<T> (child);
                if (grandChild != null)
                    return grandChild;
            }

            return null;
        }

        void OnNewCommandExecuted (object sender, ExecutedRoutedEventArgs e) => App.ShowStandaloneWindow ();

        void OnOpenCommandExecuted (object sender, ExecutedRoutedEventArgs e) => App.OpenWorkbook ();

        void OnSaveCommandExecuted (object sender, ExecutedRoutedEventArgs e) => Save (SaveOperation.Save);

        void OnSaveAsCommandExecuted (object sender, ExecutedRoutedEventArgs e) => Save (SaveOperation.SaveAs);

        void OnCloseCommandExecuted (object sender, ExecutedRoutedEventArgs e) => Close ();

        void CanSaveCommandExecute (object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanSave;
            e.Handled = true;
        }

        void CanNewCommandExecute (object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Session.SessionKind == ClientSessionKind.Workbook;
            e.Handled = true;
        }

        void CanOpenCommandExecute (object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Session.SessionKind == ClientSessionKind.Workbook;
            e.Handled = true;
        }

        async void OnExecuteAllCommandExecuted (object sender, ExecutedRoutedEventArgs args)
        {
            try {
                await Session.WorkbookPageViewModel.EvaluateAllAsync ();
            } catch (Exception e) {
                Log.Error (TAG, e);
            }
        }

        void CanExecuteAllCommandExecute (object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = Session.SessionKind == ClientSessionKind.Workbook && Session.CanEvaluate;
            args.Handled = true;
        }

        void OnRemovePackageCommandExecuted (object sender, ExecutedRoutedEventArgs args)
        {
            var package = (args.Parameter as NuGetPackageNode)?.RepresentedObject as InteractivePackage;

            if (package != null)
                Session.Workbook.Packages.RemovePackage (package);
        }

        void OnAddPackageCommandExecuted (object sender, ExecutedRoutedEventArgs args)
        {
            if (Session.CanAddPackages)
                new PackageManagerWindow (Session) { Owner = this }.ShowDialog ();
        }

        void CanExecuteAddPackageCommand (object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = Session.CanAddPackages;
            args.Handled = true;
        }

        void HandleEditorEvent (EditorEvent evnt)
        {
            if (evnt is IDocumentDirtyEvent)
                IsDirty = true;
            else if (evnt is FocusEvent)
                menuManager.Update (Session.Workbook.EditorHub);
        }

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
        {
            switch (evnt.Kind) {
            case ClientSessionEventKind.SessionAvailable:
                OnSessionAvailable ();
                break;
            // TODO: When pages branch lands make sure this detects changes to all pages
            case ClientSessionEventKind.SessionTitleUpdated:
                NoteRecentDocument ();
                if (Session.Workbook.IsDirty)
                    IsDirty = true;
                break;
            }
        }

        void IObserver<ClientSessionEvent>.OnError (Exception error)
        {
        }

        void IObserver<ClientSessionEvent>.OnCompleted ()
            => Close ();

        void OnSessionAvailable ()
        {
            webView = new XcbWebView (replWebView) { IsContextMenuEnabled = false };

            Session.InitializeAsync (new WorkbookWebPageViewHost (webView)).Forget ();

            Session.Workbook.EditorHub.Events.Subscribe (
                new Observer<EditorEvent> (HandleEditorEvent));

            preferenceSubscription = PreferenceStore.Default.Subscribe (ObservePreferenceChange);
            ViewModel.PropertyChanged += InspectModel_PropertyChanged;

            webView.NewWindow += ReplXcbWebView_NewWindow;
        }

        private void ReplXcbWebView_NewWindow (string url, string targetFrame, ref bool cancel)
        {
            cancel = HandleNavigation (new Uri (url));
        }

        void OnOutlineSelected (object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TableOfContentsNode toc)
                ((XcbWorkbookPageView)Session.WorkbookPageViewModel).ScrollToElementWithId (toc.Id);
        }

        void HandleTabControlMouseDown (object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove ();
        }
    }
}