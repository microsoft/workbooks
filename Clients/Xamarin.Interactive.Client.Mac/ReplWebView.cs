//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Kenneth Pouncey <kenneth.pouncey@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using CoreGraphics;
using CoreServices;
using AppKit;
using ObjCRuntime;
using WebKit;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Preferences;
using Xamarin.Interactive.Client.Mac.WebDocument.MapView;
using Xamarin.Interactive.Unified;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Client.Mac
{
    [Register (nameof (ReplWebView))]
    sealed class ReplWebView : WebView
    {
        const string TAG = nameof (ReplWebView);

        IDisposable preferenceSubscription;

        ReplWebView (IntPtr handle) : base (handle)
        {
        }

        [Export ("initWithCoder:")]
        ReplWebView (NSCoder coder) : base (coder)
        {
        }

        public XcbWebView XcbWebView { get; private set; }
        public WebInspector WebInspector { get; private set; }

        public override void AwakeFromNib ()
        {
            XcbWebView = new XcbWebView (this);

            UIDelegate = new XIWebUIDelegate ();
            PolicyDelegate = new XIWebPolicyDelegate ();

            RegisterViewClass<XIMapViewWebDocumentView, XIMapViewWebDocumentRepresentation> (
                "application/x-inspector-map-view");

            if (WebKitPrefs.DeveloperExtrasEnabled) {
                using (NativeExceptionHandler.Trap ()) {
                    try {
                        WebInspector = this.GetInspector ();
                    } catch (Exception e) {
                        Log.Error (TAG, "private WebKit API may have been removed", e);
                    }
                }

                switch (Prefs.Developer.StartupWebInspectorPane.GetValue ()) {
                case Prefs.Developer.WebInspectorPane.Console:
                    WebInspector?.ShowConsole (this);
                    break;
                case Prefs.Developer.WebInspectorPane.ElementTree:
                    WebInspector?.Show (this);
                    break;
                }
            }
        }

        public void SubscribeToPreferences ()
        {
            preferenceSubscription = PreferenceStore.Default.Subscribe (OnPreferenceChange);

            OnPreferenceChange (new PreferenceChange (Prefs.UI.Font.Key));
            OnPreferenceChange (new PreferenceChange (Prefs.Developer.MonitorCssChanges.Key));
        }

        public void UnsubscribeFromPreferences ()
        {
            preferenceSubscription?.Dispose ();
            preferenceSubscription = null;
        }

        #region Fonts

        void OnPreferenceChange (PreferenceChange change)
        {
            if (change.Key == Prefs.UI.Font.Key)
                UpdateFont ();

            if (change.Key == Prefs.Developer.MonitorCssChanges.Key) {
                if (Prefs.Developer.MonitorCssChanges.GetValue () &&
                    WebKitPrefs.DeveloperExtrasEnabled)
                    StartMonitoringExternalCssChanges ();
                else
                    StopMonitoringExternalCssChanges ();
            }
        }

        void UpdateFont ()
        {
            var fontSize = (int)Math.Round (Prefs.UI.Font.GetSize ());
            Preferences.DefaultFixedFontSize = fontSize;
            Preferences.DefaultFontSize = fontSize;

            Preferences.FixedFontFamily = NSFont.UserFixedPitchFontOfSize (0).FontName;
            Preferences.SansSerifFontFamily = NSFont.UserFontOfSize (0).FontName;
        }

        #endregion

        #region WebDocumentView / WebDocumentRepresentation (e.g. embedded MKMapView)

        readonly HashSet<Type> registeredViewClasses = new HashSet<Type> ();

        void RegisterViewClass<TWebDocumentView, TWebDocumentRepresentation> (string mimeType)
            where TWebDocumentRepresentation : WebDocumentRepresentation
            // WebDocumentView is an informal protocol and not bound in XM so we can't constrain
        {
            RegisterViewClass (
                new Class (typeof(TWebDocumentView)),
                new Class (typeof(TWebDocumentRepresentation)),
                mimeType);

            registeredViewClasses.Add (typeof(TWebDocumentView));
        }

        NSView RegisteredViewHitTest (NSView rootView, CGPoint point)
        {
            foreach (var subview in rootView.Subviews) {
                var hitView = registeredViewClasses.Contains (subview.GetType ())
                    ? subview.HitTest (point)
                    : RegisteredViewHitTest (subview, point);
                if (hitView != null)
                    return hitView;
            }

            return null;
        }

        public override NSView HitTest (CGPoint aPoint)
        {
            // WebView does some really ugly stuff hijacking hit testing and mouse events
            // in general. Here we implement our own hit testing by recursing through the
            // WebView subview hierarchy looking for a view type that has been registered
            // via RegisterViewClass<,>. If one is found, perform a hit test directly on
            // it and return its result. This allows a responding view to receive all of
            // its usual events. The document view however must also override HitTest to
            // perform a custom test since the default will be influenced by WebView.
            return RegisteredViewHitTest (this, aPoint) ?? base.HitTest (aPoint);
        }

        #endregion

        class XIWebPolicyDelegate : WebPolicyDelegate
        {
            bool initialNavigationPerformed;

            public override void DecidePolicyForNavigation (
                WebView webView,
                NSDictionary actionInformation,
                NSUrlRequest request,
                WebFrame frame,
                NSObject decisionToken)
            {
                if (!initialNavigationPerformed) {
                    WebView.DecideUse (decisionToken);
                    initialNavigationPerformed = true;
                    return;
                }

                var originalRequestUrl = (NSUrl)actionInformation [WebActionOriginalUrlKey];

                if (originalRequestUrl.Scheme == "data") {
                    WebView.DecideUse (decisionToken);
                    return;
                }

                // Let iframes do their own navigation handling.
                if (frame != webView.MainFrame) {
                    if (originalRequestUrl.Scheme == "about") {
                        WebView.DecideUse (decisionToken);
                    } else {
                        NSWorkspace.SharedWorkspace.OpenUrl (originalRequestUrl);
                        WebView.DecideIgnore (decisionToken);
                    }
                    return;
                }

                if (ScrollToElementWithId (webView, request.Url.Fragment)) {
                    WebView.DecideIgnore (decisionToken);
                    return;
                }

                NSUrl workspaceUrl = null;

                try {
                    var resourceAction = ClientApp
                        .SharedInstance
                        .WebServer
                        .TryGetLocalResourcePath (
                            originalRequestUrl,
                            out var localPath);

                    switch (resourceAction) {
                    case ClientWebServer.ResourceAction.WorkbookResource:
                        workspaceUrl = NSUrl.FromFilename (localPath);
                        break;
                    case ClientWebServer.ResourceAction.ExternalResource:
                        workspaceUrl = originalRequestUrl;
                        break;
                    }
                } catch {
                    workspaceUrl = originalRequestUrl;
                }

                if (workspaceUrl != null)
                    NSWorkspace.SharedWorkspace.OpenUrl (workspaceUrl);

                WebView.DecideIgnore (decisionToken);
            }

            public override void DecidePolicyForNewWindow (
                WebView webView,
                NSDictionary actionInformation,
                NSUrlRequest request,
                string newFrameName,
                NSObject decisionToken)
            {
                WebView.DecideIgnore (decisionToken);

                if (!ScrollToElementWithId (webView, request.Url.Fragment))
                    NSWorkspace.SharedWorkspace.OpenUrl (request.Url);
            }

            bool ScrollToElementWithId (WebView webView, string fragment)
            {
                // scroll to fragment/IDs via JS
                if (!string.IsNullOrEmpty (fragment)) {
                    (((webView as ReplWebView)
                        ?.Window
                        ?.WindowController as SessionWindowController)
                        ?.Session
                        ?.WorkbookPageViewModel as XcbWorkbookPageView)
                        ?.ScrollToElementWithId (fragment);
                    return true;
                }

                return false;
            }
        }

        sealed class XIWebUIDelegate : WebUIDelegatePrivate
        {
            // WebUIDelegatePrivate.h
            const int WebMenuItemTagInspectElement = 2024;

            readonly NSMenuItem [] menuItems = {
                new NSMenuItem ("Cut", new Selector ("cut:"), "x") {
                    KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask
                },
                new NSMenuItem ("Copy", new Selector ("copy:"), "c") {
                    KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask
                },
                new NSMenuItem ("Paste", new Selector ("paste:"), "p") {
                    KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask
                },
                NSMenuItem.SeparatorItem,
                NSMenuItem.SeparatorItem
            };

            public override NSMenuItem [] UIGetContextMenuItems (
                WebView sender, NSDictionary forElement, NSMenuItem [] defaultMenuItems)
            {
                menuItems [menuItems.Length - 1] = defaultMenuItems
                    .FirstOrDefault (dmi => dmi.Tag == WebMenuItemTagInspectElement)
                        ?? NSMenuItem.SeparatorItem;
                return menuItems;
            }

            public override NSEventModifierMask UIGetDragDestinationActionMask (
                WebView webView, NSDraggingInfo draggingInfo) => 0;

            static readonly NSString messageLevelKey = new NSString ("MessageLevel");
            static readonly NSString messageSourceKey = new NSString ("MessageSource");
            static readonly NSString messageKey = new NSString ("message");
            static readonly NSString lineNumberKey = new NSString ("lineNumber");
            static readonly NSString sourceUrlKey = new NSString ("sourceURL");

            public override void AddMessageToConsole (WebView webView, NSDictionary message, NSString source)
            {
                // message = {
                //    MessageLevel = ErrorMessageLevel;
                //    MessageSource = ConsoleAPIMessageSource;
                //    columnNumber = 3534;
                //    lineNumber = 7;
                //    message = "Here are the modules that depend on it:";
                //    sourceURL = "http://localhost:63693/monaco-editor/vs/loader.js";
                //}

                NSObject value;

                string messageLevel = null;
                if (message.TryGetValue (messageLevelKey, out value))
                    messageLevel = value as NSString;

                LogLevel level;
                switch (messageLevel) {
                case "LogMessageLevel":
                case "InfoMessageLevel":
                    level = LogLevel.Info;
                    break;
                case "WarningMessageLevel":
                    level = LogLevel.Warning;
                    break;
                case "ErrorMessageLevel":
                    level = LogLevel.Error;
                    break;
                // case "DebugMessageLevel":
                default:
                    level = LogLevel.Debug;
                    break;
                }

                string tag = null;
                if (message.TryGetValue (messageSourceKey, out value))
                    tag = value as NSString;
                if (tag == null)
                    tag = "UnknownWebKitMessageSource";

                int lineNumber = 0;
                if (message.TryGetValue (lineNumberKey, out value)) {
                    var nsLineNumber = value as NSNumber;
                    if (nsLineNumber != null)
                        lineNumber = nsLineNumber.Int32Value;
                }

                string sourceUrl = null;
                if (message.TryGetValue (sourceUrlKey, out value))
                    sourceUrl = value as NSString;

                string messageString = null;
                if (message.TryGetValue (messageKey, out value))
                    messageString = value as NSString;
                if (messageString == null)
                    messageString = "(null message)";

                Log.Commit (
                    level,
                    LogFlags.SkipTelemetry,
                    tag,
                    messageString,
                    callerFilePath: sourceUrl,
                    callerLineNumber: lineNumber);
            }
        }

        #region CSS Dev Monitoring

        FSEventStream fsEventStream;

        void StartMonitoringExternalCssChanges ()
        {
            if (fsEventStream != null)
                return;

            var monitorPaths = new [] { ClientApp.SharedInstance.WebServer.SourceBasePath.FullPath };

            Log.Info (TAG, $"Starting CSS monitor for paths: {string.Join (", ", monitorPaths)}");

            fsEventStream = new FSEventStream (
                monitorPaths,
                TimeSpan.FromSeconds (1),
                FSEventStreamCreateFlags.WatchRoot | FSEventStreamCreateFlags.FileEvents);
            fsEventStream.Events += HandleFSEventStreamEvents;
            fsEventStream.ScheduleWithRunLoop (NSRunLoop.Current);
            fsEventStream.Start ();
        }

        void StopMonitoringExternalCssChanges ()
        {
            if (fsEventStream != null) {
                Log.Info (TAG, "Stopping CSS monitor");

                fsEventStream.Stop ();
                fsEventStream.Dispose ();
                fsEventStream = null;
            }
        }

        void HandleFSEventStreamEvents (object sender, FSEventStreamEventsArgs args)
        {
            var xiexports = WindowScriptObject?.ValueForKey (new NSString ("xiexports")) as WebScriptObject;
            var cssMonitor = xiexports?.ValueForKey (new NSString ("cssMonitor")) as WebScriptObject;
            if (cssMonitor == null)
                return;

            var basePath = ClientApp.SharedInstance.WebServer.SourceBasePath.FullPath;

            foreach (var fsEvent in args.Events)
                cssMonitor.CallWebScriptMethod ("notifyFileEvent", new NSObject [] {
                    new NSString (fsEvent.Path.Substring (basePath.Length))
                });
        }

        #endregion
    }
}