//
// XcbWebView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

using mshtml;
using Microsoft.Win32;

using Xamarin.CrossBrowser.Wpf.Internal;

namespace Xamarin.CrossBrowser
{
    public delegate void NewWindowEventHandler (string url, string targetFrame, ref bool cancel);

    public sealed class XcbWebView
    {
        readonly WebBrowser webBrowser;
        readonly XcbWebBrowserSite webBrowserSite;
        readonly Window parentWindow;
        public event EventHandler FinishedLoad;
        public event NewWindowEventHandler NewWindow;

        public ScriptContext ScriptContext { get; }

        public bool IsContextMenuEnabled {
            get { return webBrowserSite.IsContextMenuEnabled; }
            set { webBrowserSite.IsContextMenuEnabled = value; }
        }

        public XcbWebView (WebBrowser webBrowser)
        {
            if (webBrowser == null)
                throw new ArgumentNullException (nameof (webBrowser));

            this.webBrowser = webBrowser;
            parentWindow = Window.GetWindow (webBrowser);

            webBrowserSite = XcbWebBrowserSite.Bind (webBrowser);

            ScriptContext = new ScriptContext (webBrowser);

            webBrowser.LoadCompleted += OnLoadCompleted;
        }

        void OnLoadCompleted (object sender, NavigationEventArgs navigationEventArgs)
        {
            var serviceProvider = (Com.IServiceProvider)webBrowser.Document;
            if (serviceProvider != null) {
                var serviceGuid = new Guid ("0002DF05-0000-0000-C000-000000000046");
                var iid = typeof (SHDocVw.WebBrowser).GUID;
                IntPtr ppvObject;
                if (serviceProvider.QueryService (ref serviceGuid, ref iid, out ppvObject) ==
                    Native.S_OK) {
                    var comWebBrowser = (SHDocVw.WebBrowser_V1)
                        Marshal.GetObjectForIUnknown (ppvObject);
                    if (comWebBrowser != null)
                        comWebBrowser.NewWindow += ComWebBrowser_NewWindow;
                }
            }

            ScriptContext.Initialize ();
            FinishedLoad?.Invoke (this, EventArgs.Empty);
        }

        void ComWebBrowser_NewWindow (
            string URL,
            int Flags,
            string TargetFrameName,
            ref object PostData,
            string Headers,
            ref bool Processed)
            => NewWindow?.Invoke (URL, TargetFrameName, ref Processed);

        public HtmlDocument Document => new HtmlDocument (ScriptContext, (IHTMLDocument2)webBrowser.Document);

        public void Focus ()
        {
            (webBrowser as IKeyboardInputSink)?.TabInto (new TraversalRequest (FocusNavigationDirection.First));
            webBrowser.Dispatcher.BeginInvoke (System.Windows.Threading.DispatcherPriority.Input, (Action) (FocusDocument));
        }

        void FocusDocument ()
        {
            if (parentWindow.IsActive)
                ((IHTMLDocument4) webBrowser.Document).focus ();
        }

        public static Version InternetExplorerVersion { get; }

        static XcbWebView ()
        {
            using (var ieKey = Registry.LocalMachine.OpenSubKey (
                @"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                RegistryRights.QueryValues)) {
                var versionString = ieKey.GetValue ("svcVersion") as string;
                Version version = null;
                if (versionString != null)
                    Version.TryParse(versionString, out version);
                InternetExplorerVersion = version ?? new Version ();
            }

            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime) {
                SetFeature ("FEATURE_BROWSER_EMULATION", 11000 /* Force IE 11 */);
                EnableFeature ("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION");
                EnableFeature ("FEATURE_AJAX_CONNECTIONEVENTS");
                EnableFeature ("FEATURE_GPU_RENDERING");
                EnableFeature ("FEATURE_WEBOC_DOCUMENT_ZOOM");
                EnableFeature ("FEATURE_NINPUT_LEGACYMODE", false);
            }
        }

        static void EnableFeature (string name, bool value = true)
            => SetFeature (name, value ? 1 : 0);

        static void SetFeature (string name, int value)
            => Registry.SetValue (
                @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\" + name,
                Process.GetCurrentProcess ().MainModule.FileName,
                value,
                RegistryValueKind.DWord);

        public void Navigate (Uri uri) => webBrowser.Source = uri;
    }
}