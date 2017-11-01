//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Foundation;

namespace WebKit
{
    [BaseType (typeof (WebView))]
    [Category]
    interface WebViewPrivate
    {
        [Export ("inspector")]
        WebInspector GetInspector ();
    }

    [BaseType (typeof (WebPreferences))]
    [Category]
    interface WebPreferencesPrivate
    {
        [Export ("developerExtrasEnabled")]
        bool GetDeveloperExtrasEnabled ();

        [Export ("setDeveloperExtrasEnabled:")]
        void SetDeveloperExtrasEnabled (bool enabled);
    }

    [BaseType (typeof (WebUIDelegate))]
    [Synthetic]
    interface WebUIDelegatePrivate
    {
        [Export ("webView:addMessageToConsole:withSource:")]
        void AddMessageToConsole (WebView webView, NSDictionary message, NSString source);
    }

    [BaseType (typeof (NSObject))]
    [DisableDefaultCtor]
    interface WebInspector
    {
        [Export ("show:")]
        void Show (NSObject sender);

        [Export ("showConsole:")]
        void ShowConsole (NSObject sender);

        [Export ("close:")]
        void Close (NSObject sender);

        [Export ("attach:")]
        void Attach (NSObject sender);

        [Export ("detach:")]
        void Detach (NSObject sender);

        [Export ("isDebuggingJavaScript")]
        bool IsDebuggingJavaScript { get; }

        [Export ("toggleDebuggingJavaScript:")]
        void ToggleDebuggingJavaScript (NSObject sender);

        [Export ("startDebuggingJavaScript:")]
        void StartDebuggingJavaScript (NSObject sender);

        [Export ("stopDebuggingJavaScript:")]
        void StopDebuggingJavaScript (NSObject sender);

        [Export ("isJavaScriptProfilingEnabled")]
        bool IsJavaScriptProfilingEnabled {
            get;
            [Bind ("setJavaScriptProfilingEnabled:") set;
        }

        [Export ("isTimelineProfilingEnabled")]
        bool IsTimelineProfilingEnabled {
            get;
            [Bind ("setTimelineProfilingEnabled:") set;
        }

        [Export ("isProfilingJavaScript")]
        bool IsProfilingJavaScript { get; }

        [Export ("toggleProfilingJavaScript:")]
        void ToggleProfilingJavaScript (NSObject sender);

        [Export ("startProfilingJavaScript:")]
        void StartProfilingJavaScript (NSObject sender);

        [Export ("stopProfilingJavaScript:")]
        void StopProfilingJavaScript (NSObject sender);

        [Export ("isOpen")]
        bool IsOpen { get; }
    }
}