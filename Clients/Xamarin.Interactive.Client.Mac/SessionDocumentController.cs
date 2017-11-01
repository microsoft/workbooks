//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Foundation;
using AppKit;
using ObjCRuntime;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.Client.Mac
{
    sealed class SessionDocumentController : NSDocumentController
    {
        const string TAG = nameof (SessionDocumentController);

        // these must match type names or UTIs in the Info.plist
        const string WorkbookDocumentUTI = "com.xamarin.workbook";
        const string InspectorDocumentTypeName = "Xamarin Inspector Session";

        public static new SessionDocumentController SharedDocumentController
            => (SessionDocumentController)NSDocumentController.SharedDocumentController;

        readonly ClientSessionController<SessionDocument> clientSessionController
            = new ClientSessionController<SessionDocument> ();

        public new SessionDocument DocumentForWindow (NSWindow window)
            => window == null ? null : base.DocumentForWindow (window) as SessionDocument;

        public SessionDocument OpenDocument (ClientSessionUri uri)
            => OpenDocument (new NSUrl (uri));

        public SessionDocument OpenDocument (NSUrl url)
        {
            // error is ignored here since OpenDocument will present it to the user
            NSError error;
            return OpenDocument (url, true, out error) as SessionDocument;
        }

        public void OpenUntitledDocument (AgentType? agentType = null)
        {
            var keyWindow = NSApplication.SharedApplication.KeyWindow;
            if (keyWindow is NewWorkbookWindow)
                return;

            var newWorkbookWindowController = (NSWindowController)NSStoryboard
                .FromName ("Main", NSBundle.MainBundle)
                .InstantiateControllerWithIdentifier ("NewWorkbookWindowController");

            if (keyWindow is SessionWindow)
                keyWindow.BeginSheet (
                    newWorkbookWindowController.Window,
                    result => { });
            else
                newWorkbookWindowController.Window.MakeKeyAndOrderFront (this);

            if (agentType.HasValue)
                ((NewWorkbookViewController)newWorkbookWindowController.ContentViewController)
                    .SelectedAgentType = agentType.Value;
        }

        public override NSObject OpenUntitledDocument (bool displayDocument, out NSError outError)
        {
            outError = AppDelegate.SuppressionNSError;

            if (!CommandLineTool.TestDriver.ShouldRun)
                OpenUntitledDocument ();

            return null;
        }

        public override string TypeForUrl (NSUrl url, out NSError outError)
        {
            outError = null;

            var sessionUri = (ClientSessionUri)new Uri (url.ToString ());

            switch (sessionUri.SessionKind) {
            case ClientSessionKind.LiveInspection:
                return InspectorDocumentTypeName;
            default:
                return WorkbookDocumentUTI;
            }
        }

        public override NSObject OpenDocument (NSUrl url, bool displayDocument, out NSError outError)
        {
            outError = AppDelegate.SuppressionNSError;

            if (CommandLineTool.TestDriver.ShouldRun)
                return null;

            SessionDocument document = null;

            try {
                ClientSessionUri sessionUri;

                try {
                    sessionUri = (ClientSessionUri)new Uri (url.ToString ());
                } catch (Exception e) {
                    throw new UriFormatException (Catalog.GetString (
                        "Invalid or unsupported URI."), e);
                }

                if (clientSessionController.TryGetApplicationState (
                    sessionUri,
                    out document) && document != null) {
                    document.MakeKeyAndOrderFront (this);
                    return document;
                }

                document = base.OpenDocument (url, displayDocument, out outError) as SessionDocument;

                if (outError != null && outError != AppDelegate.SuppressionNSError)
                    throw new NSErrorException (outError);

                if (document != null)
                    clientSessionController.AddSession (document.Session, document);

                return document;
            } catch (Exception e) {
                if (document != null) {
                    try {
                        document.Close ();
                    } catch (Exception ee) {
                        Log.Error (TAG, "Exception caught when trying to close document " +
                            "due to exception when opening document", ee);
                    }
                }

                e.ToUserPresentable ($"“{url}” could not be opened.").Present ();
            }

            return null;
        }

        public void CloseDocument (SessionDocument document)
        {
            if (document == null)
                throw new ArgumentNullException (nameof (document));

            if (document.Session != null) {
                document.Session.Dispose ();
                clientSessionController.RemoveSession (document.Session);
            }
        }

        public override bool RespondsToSelector (Selector sel)
        {
            switch (sel.Name) {
            case "newDocument:":
            case "openDocument:":
                return ClientInfo.Flavor == ClientFlavor.Workbooks;
            }

            return base.RespondsToSelector (sel);
        }

        public override nint MaximumRecentDocumentCount
            => ClientInfo.Flavor == ClientFlavor.Workbooks
                ? this.GetSuperMaximumRecentDocumentCount ()
                : 0;
    }
}