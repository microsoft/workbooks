//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class WorkbookWebPageViewHost : IWorkbookPageHost
    {
        readonly XcbWebView webView;

        public WorkbookWebPageViewHost (XcbWebView webView)
            => this.webView = webView
                ?? throw new ArgumentNullException (nameof (webView));

        public IEnumerable<ClientSessionTaskDelegate> GetClientSessionInitializationTasks (Uri clientWebServerUri)
        {
            Task LoadWebViewAsync (CancellationToken cancellationToken)
            {
                var tcs = new TaskCompletionSource ();

                try {
                    void FinishedLoadHandler (object sender, EventArgs e)
                    {
                        try {
                            webView.FinishedLoad -= FinishedLoadHandler;
                            tcs.SetResult ();
                        } catch (Exception ex) {
                            tcs.SetException (ex);
                        }
                    }

                    webView.FinishedLoad += FinishedLoadHandler;
                    webView.Navigate (clientWebServerUri);
                } catch (Exception e) {
                    tcs.SetException (e);
                }

                return tcs.Task;
            }

            Task WaitForXIExportsAsync (CancellationToken cancellationToken)
            {
                var tcs = new TaskCompletionSource ();

                try {
                    webView.Document.Context.GlobalObject.xiexportsLoaded = (ScriptAction)(
                        (self, args) => tcs.SetResult ());
                } catch (Exception e) {
                    tcs.SetException (e);
                }

                return tcs.Task;
            }

            Task WaitForMonacoAsync (CancellationToken cancellationToken)
            {
                var tcs = new TaskCompletionSource ();

                try {
                    webView.Document.Context.GlobalObject.xiexports.monaco.onload (
                        (ScriptAction)((self, args) => tcs.SetResult ()));
                } catch (Exception e) {
                    tcs.SetException (e);
                }

                return tcs.Task;
            }

            yield return LoadWebViewAsync;
            yield return WaitForXIExportsAsync;
            yield return WaitForMonacoAsync;
        }

        public WorkbookPageViewModel CreatePageViewModel (
            ClientSession clientSession,
            WorkbookPage workbookPage)
            => new XcbWorkbookPageView (webView, clientSession, workbookPage);
    }
}