//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

using ObjCRuntime;
using Foundation;
using AppKit;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Workbook.LoadAndSave;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Client.Mac
{
    [Register (nameof (SessionDocument))]
    sealed class SessionDocument : NSDocument, IObserver<ClientSessionEvent>
    {
        const string TAG = nameof (SessionDocument);

        /// <summary>
        /// A bit of a hack, but <see cref="SessionWindowController"/> needs to see
        /// the instance of the document that is going to create it. Since Cocoa is
        /// actually instantiating it, we can't pass anything to a constructor. This
        /// "push/pop" hack is okay since we ensure the instantiation dance is always
        /// on the main thread. <seealso cref="SessionWindowController.WindowDidLoad"/>
        /// </summary>
        public static SessionDocument InstanceForWindowControllers { get; private set; }

        SessionWindowController sessionWindowController;

        public ClientSession Session { get; private set; }

        SessionDocument (IntPtr handle) : base (handle)
        {
        }

        public override void MakeWindowControllers ()
        {
            MainThread.Ensure ();

            InstanceForWindowControllers = this;

            sessionWindowController = (SessionWindowController)NSStoryboard
                .FromName ("Main", NSBundle.MainBundle)
                .InstantiateControllerWithIdentifier (nameof (SessionWindowController));

            InstanceForWindowControllers = null;

            Session.Subscribe (this);

            Session.Workbook.EditorHub.Events.Subscribe (
                new Observer<EditorEvent> (HandleEditorEvent));

            AddWindowController (sessionWindowController);

            Log.Info (TAG, $"Created {sessionWindowController.GetType ()}");
        }

        public void MakeKeyAndOrderFront (NSObject sender)
            => sessionWindowController?.Window?.MakeKeyAndOrderFront (sender);

        public override string DefaultDraftName => WorkbookPage.DefaultTitle;

        public override string DisplayName => Session.Title;

        public override void Close ()
        {
            SessionDocumentController.SharedDocumentController.CloseDocument (this);
            base.Close ();
        }

        #region IObserver<ClientSessionEvent>

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent value)
        {
            if (value.Kind == ClientSessionEventKind.SessionTitleUpdated && Session.Workbook.IsDirty)
                UpdateChangeCount (NSDocumentChangeType.Done);
        }

        void IObserver<ClientSessionEvent>.OnError (Exception error)
        {
        }

        void IObserver<ClientSessionEvent>.OnCompleted ()
            => Close ();

        #endregion

        #region Workbook Load & Save

        public override bool IsDocumentEdited
            => Session.SessionKind == ClientSessionKind.Workbook && base.IsDocumentEdited;

        void HandleEditorEvent (EditorEvent obj)
        {
            if (obj is ChangeEvent)
                UpdateChangeCount (NSDocumentChangeType.Done);
        }

        public override bool RespondsToSelector (Selector sel)
        {
            switch (sel.Name) {
            case "saveDocument:":
            case "saveDocumentAs:":
                return Session.SessionKind == ClientSessionKind.Workbook;
            }

            return base.RespondsToSelector (sel);
        }

        public override bool ReadFromUrl (NSUrl url, string typeName, out NSError outError)
        {
            Log.Info (TAG, $"url: {url}, typeName: {typeName}");

            outError = AppDelegate.SuppressionNSError;

            try {
                if (!url.IsFileUrl ||
                    new FilePath (url.Path).IsChildOfDirectory (NSBundle.MainBundle.ResourcePath))
                    FileUrl = null;
                else if (!WorkbookPackage.IsPossiblySupported (url.Path))
                    throw new FileNotFoundException (
                        Catalog.GetString ("The file does not exist."),
                        url.Path);
                var urlStr = url.AbsoluteString;
                Uri uri;

                // VSM 7.1.1297 shipped with a bug that double-escaped URIs. Detect this case, and
                // unescape before parsing if necessary.
                if (urlStr != null && urlStr.Contains ("assemblySearchPath=%252F")) {
                    urlStr = Uri.UnescapeDataString (urlStr);
                    uri = new Uri (urlStr);
                } else
                    uri = url;

                Session = new ClientSession ((ClientSessionUri)uri);
            } catch (Exception e) {
                e.ToUserPresentable (Catalog.Format (Catalog.GetString (
                    "“{0}” could not be opened.",
                    comment: "'{0}' is a URL "),
                    url)).Present ();
                return false;
            }

            outError = null;
            return true;
        }

        IWorkbookSaveOperation workbookSaveOperation;

        public override bool WriteSafelyToUrl (
            NSUrl url,
            string typeName,
            NSSaveOperationType saveOperation,
            out NSError outError)
        {
            Log.Info (TAG, $"url: {url}, typeName: {typeName}, saveOperation: {saveOperation}");

            outError = AppDelegate.SuppressionNSError;

            try {
                if (workbookSaveOperation == null)
                    workbookSaveOperation = Session.CreateWorkbookSaveOperation ();

                switch (saveOperation) {
                case NSSaveOperationType.Save:
                case NSSaveOperationType.InPlace:
                case NSSaveOperationType.Autosave:
                    break;
                default:
                    workbookSaveOperation.Destination = url.Path;
                    break;
                }

                Session.SaveWorkbook (workbookSaveOperation);
            } catch (Exception e) {
                e.ToUserPresentable (Catalog.Format (Catalog.GetString (
                    "“{0}” could not be saved.",
                    comment: "'{0}' is a URL"),
                    url)).Present ();
                return false;
            } finally {
                workbookSaveOperation = null;
            }

            outError = null;
            return true;
        }

        public override bool ShouldRunSavePanelWithAccessoryView => false;

        NSView CreateRow (string label, string [] items, int selectedItem, Action<int> handler)
        {
            var stackView = new NSStackView {
                Orientation = NSUserInterfaceLayoutOrientation.Horizontal,
                Distribution = NSStackViewDistribution.Fill,
                Spacing = 8
            };

            var labelView = new Views.XILabel { StringValue = label };
            labelView.SizeToFit ();
            stackView.AddView (labelView, NSStackViewGravity.Leading);

            var menuView = new NSPopUpButton ();
            menuView.AddItems (items);
            menuView.SelectItem (selectedItem);
            stackView.AddView (menuView, NSStackViewGravity.Trailing);

            menuView.Activated += (sender, e) => handler ((int)menuView.IndexOfSelectedItem);

            return stackView;
        }

        public override bool PrepareSavePanel (NSSavePanel savePanel)
        {
            workbookSaveOperation = Session.CreateWorkbookSaveOperation ();
            if (workbookSaveOperation.SupportedOptions == WorkbookSaveOptions.None)
                return true;

            var stackView = new NSStackView {
                Orientation = NSUserInterfaceLayoutOrientation.Vertical,
                Spacing = 8,
                EdgeInsets = new NSEdgeInsets (12, 12, 12, 12)
            };

            Action<NSView> addRow = row => {
                stackView.AddView (row, NSStackViewGravity.Top);
                stackView.AddConstraint (NSLayoutConstraint.Create (
                    row,
                    NSLayoutAttribute.Width,
                    NSLayoutRelation.Equal,
                    stackView,
                    NSLayoutAttribute.Width,
                    1,
                    0));
            };

            if (workbookSaveOperation.SupportedOptions.HasFlag (WorkbookSaveOptions.Archive))
                addRow (CreateRow (
                    Catalog.GetString ("Workbook Format:"),
                    new [] {
                        Catalog.GetString ("Package Directory"),
                        Catalog.GetString ("Archive")
                    },
                    workbookSaveOperation.Options.HasFlag (WorkbookSaveOptions.Archive) ? 1 : 0,
                    index => {
                        switch (index) {
                        case 0:
                            workbookSaveOperation.Options &= ~WorkbookSaveOptions.Archive;
                            break;
                        case 1:
                            workbookSaveOperation.Options |= WorkbookSaveOptions.Archive;
                            break;
                        default:
                            throw new IndexOutOfRangeException ();
                        }
                    }));

            #if false
            // stubbed UI for signing is unused for now
            if (workbookSaveOperation.SupportedOptions.HasFlag (WorkbookSaveOptions.Sign))
                addRow (CreateRow (
                    Catalog.GetString ("Signing Key:"),
                    new [] {
                        Catalog.GetString ("None")
                    },
                    0,
                    index => { }));
            #endif

            savePanel.AccessoryView = stackView;

            return true;
        }

        #endregion
    }
}