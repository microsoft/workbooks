//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Foundation;
using JavaScriptCore;

namespace Xamarin.CrossBrowser
{
    public class XcbWebView
    {
        readonly WebKit.WebView webView;

        public event EventHandler FinishedLoad;

        public XcbWebView (WebKit.WebView webView)
        {
            if (webView == null)
                throw new ArgumentNullException ("webView");

            this.webView = webView;
            this.webView.FinishedLoad += (sender, e) => {
                FinishedLoad?.Invoke (this, EventArgs.Empty);
            };
        }

        public JSContext JSContext {
            get { return JSContext.FromJSGlobalContextRef (webView.MainFrame.GlobalContext); }
        }

        public HtmlDocument Document {
            get { return WrappedObject.Wrap<HtmlDocument> (JSContext.GlobalObject.GetProperty ("document")); }
        }

        public void Navigate (Uri uri)
            => webView.MainFrame.LoadRequest (new NSUrlRequest (new NSUrl (uri.AbsoluteUri)));
    }
}