//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis.Monaco;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Events;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Rendering;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Workbook.Events;
using Xamarin.Interactive.Workbook.Models;
using Xamarin.Interactive.Workbook.Structure;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class XcbWorkbookPageView : WorkbookPageViewModel
    {
        const string TAG = nameof (XcbWorkbookPageView);

        readonly XcbWebView webView;
        readonly RendererContext rendererContext;

        #pragma warning disable 0414
        readonly dynamic xiexports;
        #pragma warning restore 0414

        Cell focusedWorkbookCell;
        Models.CodeCellState focusedCellState;

        CompletionProvider completionProvider;
        SignatureHelpProvider signatureHelpProvider;
        HoverProvider hoverProvider;

        HtmlElement outputElement;
        HtmlElement firstCellActionsArticle;

        CellView delayFocusView;
        bool delayNewCodeCellFocus;
        public bool DelayNewCodeCellFocus {
            get { return delayNewCodeCellFocus; }
            set {
                if (delayNewCodeCellFocus == value)
                    return;

                delayNewCodeCellFocus = value;

                // Focus the delayed view when no longer delaying
                if (!delayNewCodeCellFocus)
                    delayFocusView?.Focus ();

                delayFocusView = null;
            }
        }

        public XcbWorkbookPageView (
            XcbWebView webView,
            ClientSession clientSession,
            WorkbookPage workbookPage)
            : base (clientSession, workbookPage)
        {
            this.webView = webView
                ?? throw new ArgumentNullException (nameof (webView));

            void ObserveWorkbookMutationModelChanges (dynamic self, dynamic args)
                => TableOfContentsNodeExtensions.RebuildFromJavaScript (workbookPage.TableOfContents, args [0]);

            webView.Document.Context.GlobalObject.xiexports.WorkbookMutationObserver.observeModelChanges (
                (ScriptAction)ObserveWorkbookMutationModelChanges);

            rendererContext = new RendererContext (clientSession, webView.Document);
            rendererContext.MemberReferenceRequested += OnRendererContextMemberReferenceRequested;
            rendererContext.AsyncRenderComplete += (sender, e) => {
                var view = focusedCellState?.View;
                // Image rendering can happen asynchronously. If the last cell is evaluated, and
                // the focus has shifted to a new last cell, make sure the new last cell's
                // input doesn't get pushed out of the view by the image load.
                if (view != null && focusedWorkbookCell != null && focusedWorkbookCell.NextCell == null)
                    SynchronizationContext.Current.Post (o => view.Focus (), null);
            };

            clientSession.Workbook.EditorHub.Events.Subscribe (
                new Observer<EditorEvent> (HandleEditorEvent));

            var document = webView.Document;
            xiexports = document.Context.GlobalObject.xiexports;

            outputElement = document.CreateElement ("main");
            outputElement.AddCssClass ("interactive-workspace");
            outputElement.AddCssClass (HostEnvironment.OS == HostOS.macOS ? "is-mac" : "is-windows");
            outputElement.AddCssClass (
                $"session-kind-{clientSession.SessionKind.ToString ().ToLowerInvariant ()}");

            document.Body.AppendChild (outputElement);

            AppendFirstCellActions (outputElement);
        }

        public void ScrollToElementWithId (string elementId)
            => xiexports.scrollToElementWithId (elementId);

        protected override void Dispose (bool disposing)
        {
            if (!disposing)
                return;

            foreach (var codeCell in CodeCells)
                codeCell.Value.Editor.Dispose ();
        }

        protected override void OnAgentDisconnected ()
        {
            completionProvider?.Dispose ();
            completionProvider = null;

            signatureHelpProvider?.Dispose ();
            signatureHelpProvider = null;

            hoverProvider?.Dispose ();
            hoverProvider = null;
        }

        protected override void OnCompilationWorkspaceAvailable ()
        {
            Func<string, Microsoft.CodeAnalysis.Text.SourceText> getSourceTextByModelId = modelId
                => CodeCells
                    .Values
                    .Select (s => s.Editor)
                    .OfType<CodeCellEditorView> ()
                    .FirstOrDefault (e => !e.IsDisposed && e.GetMonacoModelId () == modelId)
                    ?.SourceTextContent;

            completionProvider?.Dispose ();
            signatureHelpProvider?.Dispose ();
            hoverProvider?.Dispose ();

            completionProvider = new CompletionProvider (
                ClientSession.CompilationWorkspace,
                webView.Document.Context,
                getSourceTextByModelId);

            signatureHelpProvider = new SignatureHelpProvider (
                ClientSession.CompilationWorkspace,
                webView.Document.Context,
                getSourceTextByModelId);

            hoverProvider = new HoverProvider (
                ClientSession.CompilationWorkspace,
                webView.Document.Context,
                getSourceTextByModelId);
        }

        public override Task LoadWorkbookDependencyAsync (
            string dependency,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource ();
            try {
                webView.Document.Context.GlobalObject.xiexports.ResourceLoader.loadAsync (
                    dependency,
                    (ScriptAction)((s, a) => tcs.SetResult ()));
            } catch (Exception e) {
                tcs.SetException (e);
            }
            return tcs.Task;
        }

        protected override Xamarin.Interactive.Workbook.Models.CodeCellState StartNewCodeCell ()
            => focusedCellState = base.StartNewCodeCell ();

        protected override void BindMarkdownCellToView (MarkdownCell cell)
        {
            var view = new MarkdownCellView (cell, webView.Document);

            if (!string.IsNullOrEmpty (cell.Buffer.Value))
                view.MarkdownContent = cell.Buffer.Value;

            cell.View = view;
        }

        protected override void BindCodeCellToView (CodeCell cell, Models.CodeCellState codeCellState)
        {
            var codeCellView = new CodeCellView (
                codeCellState,
                cell,
                webView.Document,
                rendererContext);

            cell.View = codeCellView;

            codeCellState.Editor = codeCellView.Editor;
            codeCellState.View = codeCellView;

            codeCellView.Events.Subscribe (new Observer<IEvent> (HandleCodeCellViewEvent));
        }

        protected override void UnbindCellFromView (ICellView cellView)
        {
            if (cellView?.Editor == null || cellView.Editor.IsDisposed)
                return;

            cellView.Editor.Dispose ();
            ClientSession.Workbook.EditorHub.RemoveEditor (cellView.Editor);

            if (cellView is CellView htmlCellView && htmlCellView?.RootElement?.ParentElement != null)
                htmlCellView.RootElement.ParentElement.RemoveChild (htmlCellView.RootElement);
        }

        protected override void InsertCellInViewModel (Cell newCell, Cell previousCell)
        {
            var view = (CellView)newCell.View;
            var previousCellElem = (previousCell?.View as CellView)?.RootElement
                                                                    ?? firstCellActionsArticle;
            var insertAfterElem = previousCellElem.NextElementSibling;
            if (insertAfterElem != null)
                outputElement.InsertBefore (view.RootElement, insertAfterElem);
            else
                outputElement.AppendChild (view.RootElement);

            // Register the view's editor
            ClientSession.Workbook.EditorHub.AddEditor (view.Editor, newCell);

            // Append UI and focus / scroll
            AppendCellActions (newCell, view.FooterElement, view.RootElement);
            if (delayNewCodeCellFocus)
                delayFocusView = view;
            else
                view.Focus ();

            var bodyElem = webView.Document.Body;
            var originalScrollTop = bodyElem.ScrollTop;

            // by default the new cell will scroll itself into view, but we want to special case the
            // synthesized article here by keeping our existing scroll position (before the cell appent)
            if (previousCellElem == firstCellActionsArticle)
                SynchronizationContext.Current.Post (o => bodyElem.ScrollTop = originalScrollTop, null);
        }

        void HandleCodeCellViewEvent (IEvent evnt)
        {
            Log.Debug (TAG, $"{evnt}");

            if (evnt is AbortEvaluationEvent)
                AbortEvaluationAsync ().Forget ();
        }

        void HandleEditorEvent (EditorEvent evnt)
        {
            CodeCells.TryGetValue (evnt.Source, out var sourceCodeCellState);

            switch (evnt) {
            case EvaluateCodeCellEvent _:
                EvaluateCodeCellAsync (sourceCodeCellState).Forget ();
                break;
            case InsertCellEvent<MarkdownCell> insertCellEvent:
                InsertOrFocusMarkdownCell (insertCellEvent.SourceCell);
                break;
            case InsertCellEvent<CodeCell> insertCellEvent:
                InsertCodeCell (insertCellEvent.SourceCell);
                break;
            case FocusEvent focusEvent:
                HandleFocusEvent (focusEvent, sourceCodeCellState);
                break;
            case FocusSiblingEditorEvent focusSiblingEditorEvent:
                HandleFocusSiblingEditor (focusSiblingEditorEvent);
                break;
            case NavigateReplHistoryEvent navigateReplHistoryEvent:
                HandleNavigateReplHistoryEvent (navigateReplHistoryEvent);
                break;
            case ChangeEvent _ when sourceCodeCellState != null &&
                ClientSession.SessionKind == ClientSessionKind.Workbook:
                sourceCodeCellState.View.IsDirty = true;
                break;
            }
        }

        void HandleFocusEvent (EditorEvent evnt, Models.CodeCellState sourceCodeCellState)
        {
            var sourceCell = ((CellEditorView)evnt.Source).Cell;

            if (focusedCellState == sourceCodeCellState && focusedWorkbookCell == sourceCell)
                return;

            focusedCellState = sourceCodeCellState;

            UpdateFocusedCellViewFooter (false);
            focusedWorkbookCell = sourceCell;
            UpdateFocusedCellViewFooter (true);
        }

        void UpdateFocusedCellViewFooter (bool focused)
        {
            if (focusedWorkbookCell == null)
                return;

            var view = (CellView)focusedWorkbookCell.View;

            if (focused)
                view.RootElement.AddCssClass ("focused");
            else {
                view.RootElement.RemoveCssClass ("focused");
                view.Editor.OnBlur ();
            }

            xiexports.WorkbookPageView_UpdateFooter (
                view.FooterElement,
                focused);
        }

        #region Workbook Cell Actions

        /// <summary>
        /// Synthesize a first <article> so that the cell actions will appear
        /// above the first actual workbook cell which allows for inserting a
        /// new workbook cell at position 0. Actual entries that follow will
        /// contain these actions as a footer.
        /// </summary>
        void AppendFirstCellActions (HtmlElement parentElem)
        {
            var document = webView.Document;
            firstCellActionsArticle = document.CreateElement ("article");
            parentElem.AppendChild (firstCellActionsArticle);

            var firstCellActionsFooter = document.CreateElement ("footer");
            firstCellActionsArticle.AppendChild (firstCellActionsFooter);
            AppendCellActions (null, firstCellActionsFooter, null);
        }

        HtmlElement CreateActionButton (
            HtmlElement cellElem,
            string title,
            string tooltip,
            string cssClass,
            string cellHoverClass,
            Action clickHandler)
        {
            var document = webView.Document;

            var button = document.CreateElement ("button", cssClass);
            if (!String.IsNullOrWhiteSpace (tooltip))
                button.SetAttribute ("title", tooltip);

            var span = document.CreateElement ("span");
            span.AppendChild (document.CreateTextNode (title));
            button.AppendChild (span);

            button.AddEventListener ("click", evnt => clickHandler ());

            if (cellElem != null && cellHoverClass != null) {
                button.AddEventListener (
                    "mouseenter",
                    evnt => cellElem.AddCssClass (cellHoverClass));

                button.AddEventListener (
                    "mouseleave",
                    evnt => cellElem.RemoveCssClass (cellHoverClass));
            }

            return button;
        }

        void AppendCellActions (Cell cell, HtmlElement parentElem, HtmlElement cellElem)
        {
            const string canFocusCellClass = "can-focus-cell";

            var document = webView.Document;
            var actions = document.CreateElement ("section", "entry-actions " + canFocusCellClass);
            parentElem.AppendChild (actions);

            if (ClientSession.SessionKind != ClientSessionKind.Workbook)
                return;

            if (cell is CodeCell codeCell) {
                var editor = (CodeCellEditorView)codeCell.View.Editor;

                actions.AppendChild (CreateActionButton (
                    cellElem,
                    title: "Run Cell",
                    tooltip: "Run all cells up to and including this cell",
                    cssClass: "run-code " + canFocusCellClass,
                    cellHoverClass: null,
                    clickHandler: editor.EvaluateViaKeyPress));

                var kbd = HostEnvironment.OS == HostOS.macOS
                    ? "<span class='kbd'>\u2318\u21A9</span>"
                    : "<span class='kbd'>Control<span class='plus'>+</span>Return</span>";

                actions.AppendChild (document.CreateElement (
                    "div",
                    @class: "hint-text requires-editor-focus " + canFocusCellClass,
                    innerHtml: "Press " + kbd + " to run"));
            }

            actions.AppendChild (document.CreateElement ("div", "break"));

            actions.AppendChild (CreateActionButton (
                cellElem,
                title: "Insert C#",
                tooltip: "Insert a new evaluatable C# cell",
                cssClass: "insert-code",
                cellHoverClass: "insert-preview",
                clickHandler: () => InsertCodeCell (cell)));

            actions.AppendChild (CreateActionButton (
                cellElem,
                title: "Insert Documentation",
                tooltip: "Insert a new documentation cell",
                cssClass: "insert-documentation",
                cellHoverClass: "insert-preview",
                clickHandler: () => InsertMarkdownCell (cell)));

            if (cell != null) {
                actions.AppendChild (CreateActionButton (
                    cellElem,
                    title: "Delete",
                    tooltip: "Delete cell",
                    cssClass: "delete",
                    cellHoverClass: "confirm-delete-hover",
                    clickHandler: () => {
                        // On Windows, ConfirmDeleteCell dialog prevents the other click
                        // handler from processing in time, so it's possible for two cells
                        // to have outlines (one blue, one red). Force the candidate deleted
                        // cell to get focus first.
                        cell.View.Focus (scrollIntoView: false);
                        ConfirmDeleteCell (cell, cellElem);
                    }));

                cellElem?.AddEventListener ("click", evnt => {
                    var targetElem = evnt.Target as HtmlElement;
                    if (targetElem != null && (
                        targetElem == cellElem || targetElem.HasCssClass (canFocusCellClass)))
                        cell.View.Focus (scrollIntoView: false);
                });
            }
        }

        void ConfirmDeleteCell (Cell cell, HtmlElement cellElem)
        {
            cellElem.AddCssClass ("confirm-delete");

            ClientSession.ViewControllers.Messages.PushMessage (
                Message.CreateInfoAlert (
                    Catalog.GetString ("Are you sure you want to delete this cell?"),
                    Catalog.GetString ("There is currently no way to undo this action."))
                .WithAction (new MessageAction (
                    MessageActionKind.Affirmative,
                    "delete",
                    Catalog.GetString ("Delete")))
                .WithAction (new MessageAction (
                    MessageActionKind.Negative,
                    "cancel",
                    Catalog.GetString ("Cancel")))
                .WithActionResponseHandler ((message, action) => {
                    message.Dispose ();
                    cellElem.RemoveCssClass ("confirm-delete");
                    if (action.Id == "delete")
                        DeleteCell (cell);
                }));
        }

        #endregion

        #region Evaluation Rendering

        async void OnRendererContextMemberReferenceRequested (object sender, MemberReferenceRequestArgs args)
        {
            // TODO: Use this in HtmlRendererBase.CreateRenderedTypeNameElement
            // Trim the monkey from submission-declared types
            var interactiveObjectTypeName = args.InteractiveObjectType.Name;
            if (interactiveObjectTypeName.StartsWith ("🐵", StringComparison.Ordinal))
                interactiveObjectTypeName = interactiveObjectTypeName.Substring (
                    interactiveObjectTypeName.IndexOf ('+') + 1);

            var buffer = new StringWriter ();
            var writer = new CSharpWriter (buffer) { WriteLanguageKeywords = true };
            writer.VisitTypeSpec (TypeSpec.Parse (interactiveObjectTypeName));
            var interactiveObjectTypeStr = buffer.ToString ();

            var eval = "var selectedMember = " +
                $"GetObject<{interactiveObjectTypeStr}> " +
                $"(0x{args.InteractiveObject.RepresentedObjectHandle:x})";
            if (args.MemberName != null)
                eval += $".{args.MemberName}";

            await EvaluateAsync (eval);
        }

        #endregion

        #region Workbook Cell Navigation

        void HandleFocusSiblingEditor (FocusSiblingEditorEvent evnt)
        {
            switch (evnt.Which) {
            case FocusSiblingEditorEvent.WhichEditor.Previous:
                FocusPreviousCell ();
                break;
            case FocusSiblingEditorEvent.WhichEditor.Next:
                FocusNextCell ();
                break;
            }
        }

        bool FocusPreviousCell ()
        {
            var previousCellView = ClientSession.Workbook.EditorHub.FocusedEditorState?.PreviousCell?.View;
            if (previousCellView == null)
                return false;

            previousCellView.Focus ();
            previousCellView.Editor.SetCursorPosition (AbstractCursorPosition.DocumentEnd);
            return true;
        }

        bool FocusNextCell ()
        {
            var nextCellView = ClientSession.Workbook.EditorHub?.FocusedEditorState?.NextCell?.View;
            if (nextCellView == null)
                return false;

            nextCellView.Focus ();
            nextCellView.Editor.SetCursorPosition (AbstractCursorPosition.FirstLineEnd);
            return true;
        }

        #endregion

        #region History

        void HandleNavigateReplHistoryEvent (NavigateReplHistoryEvent evnt)
            => evnt.Handled =
                evnt.Cursor.Line == 0 &&
                evnt.Cursor.Character == 0 &&
                ClientSession.ViewControllers.ReplHistory != null &&
                (evnt.NavigatePrevious ? ReplHistoryPrevious () : ReplHistoryNext ());

        bool ReplHistoryPrevious ()
        {
            if (!ClientSession.ViewControllers.ReplHistory.IsPreviousAvailable)
                return false;

            var cell = focusedCellState?.Cell;
            if (cell == null)
                return false;

            ClientSession.ViewControllers.ReplHistory.Update (cell.Buffer.Value);
            cell.Buffer.Value = ClientSession.ViewControllers.ReplHistory.Previous ();

            return true;
        }

        bool ReplHistoryNext ()
        {
            if (!ClientSession.ViewControllers.ReplHistory.IsNextAvailable)
                return false;

            var cell = focusedCellState?.Cell;
            if (cell == null)
                return false;

            ClientSession.ViewControllers.ReplHistory.Update (cell.Buffer.Value);
            cell.Buffer.Value = ClientSession.ViewControllers.ReplHistory.Next ();

            return true;
        }

        #endregion
    }
}