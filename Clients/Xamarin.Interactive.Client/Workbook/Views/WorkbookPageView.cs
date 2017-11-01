//
// WorkbookPageView.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Xamarin.CrossBrowser;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Monaco;
using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.Events;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Rendering;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Workbook.Events;
using Xamarin.Interactive.Workbook.Models;

namespace Xamarin.Interactive.Workbook.Views
{
    sealed class WorkbookPageView : IObserver<ClientSessionEvent>, IDisposable
    {
        const string TAG = nameof (WorkbookPageView);

        readonly ClientSession clientSession;
        readonly XcbWebView webView;
        readonly WorkbookPage workbookPage;
        readonly RendererContext rendererContext;
        readonly Inhibitor evaluationInhibitor = new Inhibitor ();

        #pragma warning disable 0414
        readonly dynamic xiexports;
        #pragma warning restore 0414

        Cell focusedWorkbookCell;
        CodeCellState focusedCellState;
        ImmutableDictionary<IEditor, CodeCellState> codeCells =
            ImmutableDictionary<IEditor, CodeCellState>.Empty;

        CompletionProvider completionProvider;
        SignatureHelpProvider signatureHelpProvider;
        HoverProvider hoverProvider;

        HtmlElement outputElement;
        HtmlElement firstCellActionsArticle;

        sealed class Inhibitor : IDisposable
        {
            int inhibitions;

            public bool IsInhibited => inhibitions > 0;

            public IDisposable Inhibit ()
            {
                MainThread.Ensure ();
                inhibitions++;
                return this;
            }

            public void Dispose ()
            {
                MainThread.Ensure ();
                inhibitions--;
            }
        }

        public IDisposable InhibitEvaluate () => evaluationInhibitor.Inhibit ();

        public bool CanEvaluate => !evaluationInhibitor.IsInhibited;

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

        public WorkbookPageView (ClientSession clientSession, WorkbookPage workbookPage)
        {
            if (clientSession == null)
                throw new ArgumentNullException (nameof (clientSession));

            if (workbookPage == null)
                throw new ArgumentNullException (nameof (workbookPage));

            if (clientSession.WebView == null)
                throw new InvalidOperationException ("ClientSession.WebView is null");

            this.clientSession = clientSession;
            this.workbookPage = workbookPage;
            webView = clientSession.WebView;

            rendererContext = new RendererContext (clientSession);
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
            outputElement.AddCssClass (InteractiveInstallation.Default.IsMac ? "is-mac" : "is-windows");
            outputElement.AddCssClass (
                $"session-kind-{clientSession.SessionKind.ToString ().ToLowerInvariant ()}");

            document.Body.AppendChild (outputElement);

            AppendFirstCellActions (outputElement);

            LoadWorkbookCells ();

            if (workbookPage.Contents.GetFirstCell<CodeCell> () == null)
                StartNewCodeCell ();

            workbookPage
                ?.Contents
                ?.GetFirstCell<CodeCell> ()
                ?.View
                ?.Focus ();
        }

        public void Dispose ()
        {
            foreach (var codeCell in codeCells)
                codeCell.Value.Editor.Dispose ();
        }

        public void ScrollToElementWithId (string elementId)
            => xiexports.scrollToElementWithId (elementId);

        #region IObserver<ClientSessionEvent>

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
        {
            switch (evnt.Kind) {
            case ClientSessionEventKind.AgentDisconnected:
                OnAgentDisconnected ();
                break;
            case ClientSessionEventKind.CompilationWorkspaceAvailable:
                OnCompilationWorkspaceAvailable ();
                break;
            }
        }

        void IObserver<ClientSessionEvent>.OnError (Exception error)
        {
        }

        void IObserver<ClientSessionEvent>.OnCompleted ()
        {
        }

        void OnAgentDisconnected ()
        {
            completionProvider?.Dispose ();
            completionProvider = null;

            signatureHelpProvider?.Dispose ();
            signatureHelpProvider = null;

            hoverProvider?.Dispose ();
            hoverProvider = null;
        }

        void OnCompilationWorkspaceAvailable ()
        {
            PopulateCompilationWorkspace ();

            Func<string, Microsoft.CodeAnalysis.Text.SourceText> getSourceTextByModelId = modelId
                => codeCells
                    .Values
                    .Select (s => s.Editor)
                    .OfType<CodeCellEditorView> ()
                    .FirstOrDefault (e => !e.IsDisposed && e.GetMonacoModelId () == modelId)
                    ?.SourceTextContent;

            completionProvider?.Dispose ();
            signatureHelpProvider?.Dispose ();
            hoverProvider?.Dispose ();

            completionProvider = new CompletionProvider (
                clientSession.CompilationWorkspace,
                webView.Document.Context,
                getSourceTextByModelId);

            signatureHelpProvider = new SignatureHelpProvider (
                clientSession.CompilationWorkspace,
                webView.Document.Context,
                getSourceTextByModelId);

            hoverProvider = new HoverProvider (
                clientSession.CompilationWorkspace,
                webView.Document.Context,
                getSourceTextByModelId);
        }

        #endregion

        void HandleCodeCellViewEvent (IEvent evnt)
        {
            Log.Debug (TAG, $"{evnt}");

            if (evnt is AbortEvaluationEvent)
                AboutEvaluationAsync ().Forget ();
        }

        void HandleEditorEvent (EditorEvent evnt)
        {
            codeCells.TryGetValue (evnt.Source, out var sourceCodeCellState);

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
                clientSession.SessionKind == ClientSessionKind.Workbook:
                sourceCodeCellState.View.IsDirty = true;
                break;
            }
        }

        void HandleFocusEvent (EditorEvent evnt, CodeCellState sourceCodeCellState)
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

        void LoadWorkbookCells ()
        {
            foreach (var cell in workbookPage.Contents) {
                switch (cell) {
                case CodeCell codeCell:
                    LoadCodeCell (codeCell);
                    break;
                case MarkdownCell markdownCell:
                    LoadMarkdownCell (markdownCell);
                    break;
                }
            }
        }

        void LoadCodeCell (CodeCell cell)
            => InsertCodeCell (cell, cell.PreviousCell);

        void LoadMarkdownCell (MarkdownCell cell)
        {
            var view = new MarkdownCellView (cell, webView.Document) {
                MarkdownContent = cell.Buffer.Value
            };

            cell.View = view;

            clientSession.Workbook.EditorHub.AddEditor (view.Editor, cell);

            AppendCellActions (cell, view.FooterElement, view.RootElement);
            outputElement.AppendChild (view.RootElement);
        }

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

            if (clientSession.SessionKind != ClientSessionKind.Workbook)
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

                var kbd = InteractiveInstallation.Default.IsMac
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

        void InsertCellInDocumentModel (Cell newCell, Cell previousCell)
        {
            if (newCell.Document != null)
                return;

            if (previousCell == null && workbookPage.Contents.FirstCell == null)
                workbookPage.Contents.AppendCell (newCell);
            else if (previousCell == null)
                workbookPage.Contents.InsertCellBefore (
                    workbookPage.Contents.FirstCell,
                    newCell);
            else
                workbookPage.Contents.InsertCellAfter (previousCell, newCell);
        }

        void InsertCellInViewModel (Cell newCell, Cell previousCell)
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
            clientSession.Workbook.EditorHub.AddEditor (view.Editor, newCell);

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

        DocumentId GetDocumentId (CodeCell codeCell)
        {
            CodeCellState codeCellState;
            if (codeCell?.View?.Editor != null &&
                codeCells.TryGetValue (codeCell.View.Editor, out codeCellState))
                return codeCellState.DocumentId;
            return null;
        }

        void PopulateCompilationWorkspace ()
        {
            DocumentId previousDocumentId = null;

            foreach (var codeCell in workbookPage.Contents.OfType<CodeCell> ()) {
                CodeCellState codeCellState;
                var editor = codeCell?.View?.Editor;
                if (editor == null || !codeCells.TryGetValue (editor, out codeCellState))
                    continue;

                codeCellState.CompilationWorkspace = clientSession.CompilationWorkspace;
                codeCellState.DocumentId = clientSession.CompilationWorkspace.AddSubmission (
                    codeCell.CodeAnalysisBuffer.CurrentText,
                    previousDocumentId,
                    null);

                previousDocumentId = codeCellState.DocumentId;
            }
        }

        CodeCellState InsertCodeCell (Cell previousCell)
            => InsertCodeCell (new CodeCell ("csharp"), previousCell);

        CodeCellState InsertCodeCell (CodeCell newCell, Cell previousCell)
        {
            if (newCell == null)
                throw new ArgumentNullException (nameof (newCell));

            InsertCellInDocumentModel (newCell, previousCell);

            var previousCodeCell = newCell.GetPreviousCell<CodeCell> ();
            var nextCodeCell = newCell.GetNextCell<CodeCell> ();

            var codeCellState = new CodeCellState (newCell);

            var codeCellView = new CodeCellView (
                codeCellState,
                newCell,
                webView.Document,
                rendererContext);

            codeCellState.Editor = codeCellView.Editor;
            codeCellState.View = codeCellView;

            if (clientSession.CompilationWorkspace != null) {
                codeCellState.CompilationWorkspace = clientSession.CompilationWorkspace;
                codeCellState.DocumentId = clientSession.CompilationWorkspace.AddSubmission (
                    newCell.CodeAnalysisBuffer.CurrentText,
                    GetDocumentId (previousCodeCell),
                    GetDocumentId (nextCodeCell));
            }

            codeCellView.Events.Subscribe (new Observer<IEvent> (HandleCodeCellViewEvent));

            newCell.View = codeCellView;

            InsertCellInViewModel (newCell, previousCell);

            OutdateAllCodeCells (newCell);

            codeCells = codeCells.Add (codeCellState.Editor, codeCellState);

            return codeCellState;
        }

        void InsertOrFocusMarkdownCell (Cell previousCell)
        {
            var nextMarkdownCell = previousCell?.NextCell as MarkdownCell;
            if (nextMarkdownCell != null)
                nextMarkdownCell.View.Focus ();
            else
                InsertMarkdownCell (previousCell);
        }

        void InsertMarkdownCell (Cell previousCell)
        {
            var newCell = new MarkdownCell ();
            var newCellView = new MarkdownCellView (newCell, webView.Document);
            newCell.View = newCellView;

            InsertCellInDocumentModel (newCell, previousCell);
            InsertCellInViewModel (newCell, previousCell);
        }

        void ConfirmDeleteCell (Cell cell, HtmlElement cellElem)
        {
            cellElem.AddCssClass ("confirm-delete");

            clientSession.ViewControllers.Messages.PushMessage (
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

        void DeleteCell (Cell cell)
        {
            if (cell == null)
                throw new ArgumentNullException (nameof (cell));

            if (cell is CodeCell codeCell) {
                OutdateAllCodeCells (codeCell);

                if (clientSession.CompilationWorkspace != null)
                    clientSession.CompilationWorkspace.RemoveSubmission (
                        GetDocumentId (codeCell),
                        GetDocumentId (codeCell.GetNextCell<CodeCell> ()));

                codeCells = codeCells.Remove (cell.View.Editor);
            }

            var focusCell = cell.NextCell ?? cell.PreviousCell;

            cell.Document.RemoveCell (cell);

            var cellView = cell.View as CellView;

            RemoveCellView (cellView);

            focusCell?.View?.Focus ();
        }

        #endregion

        #region Evaluation & Output

        public void OutdateAllCodeCells ()
            => OutdateAllCodeCells (workbookPage.Contents.GetFirstCell<CodeCell> ());

        void OutdateAllCodeCells (CodeCell codeCell)
        {
            while (codeCell != null) {
                var view = (CodeCellView)codeCell.View;
                if (view != null)
                    view.IsOutdated = true;
                codeCell = codeCell.GetNextCell<CodeCell> ();
            }
        }

        CodeCellState StartNewCodeCell ()
        {
            return focusedCellState = InsertCodeCell (
                new CodeCell ("csharp"),
                workbookPage?.Contents?.LastCell);
        }

        async Task AboutEvaluationAsync ()
        {
            if (!clientSession.Agent.IsConnected)
                return;

            await clientSession.Agent.Api.AbortEvaluationAsync (
                clientSession.CompilationWorkspace.EvaluationContextId);
        }

        public async Task EvaluateAllAsync ()
        {
            var firstCell = workbookPage.Contents.GetFirstCell<CodeCell> ();
            if (firstCell?.View?.Editor == null)
                return;

            if (codeCells.TryGetValue (firstCell.View.Editor, out var codeCellState))
                await EvaluateCodeCellAsync (codeCellState, evaluateAll: true);
        }

        public async Task EvaluateAsync (
            string input,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (!CanEvaluate || string.IsNullOrWhiteSpace (input))
                return;

            using (InhibitEvaluate ()) {
                if (!codeCells.TryGetValue (
                    workbookPage?.Contents?.GetLastCell<CodeCell> ()?.View?.Editor,
                    out var codeCell) || codeCell.Cell.Buffer.Length > 0)
                    codeCell = StartNewCodeCell ();

                codeCell.Cell.Buffer.Value = input; // TODO: Set Mac document dirty
                switch (await CoreEvaluateCodeCellAsync (codeCell, cancellationToken)) {
                case CodeCellEvaluationStatus.ErrorDiagnostic:
                case CodeCellEvaluationStatus.Disconnected:
                    break;
                default:
                    StartNewCodeCell ();
                    break;
                }
            }
        }

        async Task EvaluateCodeCellAsync (CodeCellState codeCellState, bool evaluateAll = false)
        {
            if (!CanEvaluate || codeCellState.IsFrozen)
                return;

            using (InhibitEvaluate ())
                await DoEvaluateCodeCellAsync (codeCellState, evaluateAll);
        }

        // Only call this from EvaluateCodeCellAsync, for CanEvaluate handling
        async Task DoEvaluateCodeCellAsync (CodeCellState codeCellState, bool evaluateAll = false)
        {
            await clientSession.EnsureAgentConnectionAsync ();

            var codeCellsToEvaluate = ImmutableList<CodeCellState>.Empty;
            var originalCodeCellState = codeCellState;

            if (clientSession.ViewControllers.ReplHistory != null) {
                clientSession.ViewControllers.ReplHistory.UpdateLastAppended (
                    codeCellState.Cell.Buffer.Value.Trim ());
                clientSession.ViewControllers.ReplHistory.Save ();
            }

            var codeCell = originalCodeCellState.Cell;
            var isLastCell = codeCell.GetNextCell<CodeCell> () == null;
            var isFirstCell = codeCell.GetPreviousCell<CodeCell> () == null;

            if (isFirstCell && clientSession.SessionKind == ClientSessionKind.Workbook)
                await clientSession.Agent.Api.ResetStateAsync ();

            while (codeCell != null) {
                if (codeCells.TryGetValue (codeCell.View.Editor, out codeCellState)) {
                    var evaluateCodeCell =
                        codeCellState == originalCodeCellState ||
                        codeCellState.EvaluationCount == 0 ||
                        codeCellState.View.IsDirty ||
                        codeCellState.View.IsOutdated;

                    if (clientSession.CompilationWorkspace.HaveAnyLoadDirectiveFilesChanged (
                        codeCellState.DocumentId)) {
                        codeCellState.Cell.CodeAnalysisBuffer.Invalidate ();
                        evaluateCodeCell = true;
                    }

                    if (evaluateCodeCell)
                        codeCellsToEvaluate = codeCellsToEvaluate.Insert (
                            0, codeCellState);
                }

                codeCell = codeCell.GetPreviousCell<CodeCell> ();
            }

            codeCell = originalCodeCellState.Cell;
            var skipRemainingCodeCells = false;
            while (true) {
                codeCell = codeCell.GetNextCell<CodeCell> ();
                if (codeCell == null)
                    break;

                if (codeCells.TryGetValue (codeCell.View.Editor, out codeCellState)) {
                    if (skipRemainingCodeCells || codeCellState.AgentTerminatedWhileEvaluating)
                        skipRemainingCodeCells = true;
                    else if (evaluateAll || codeCellState.EvaluationCount > 0)
                        codeCellsToEvaluate = codeCellsToEvaluate.Add (codeCellState);
                    codeCellState.View.IsOutdated = true;
                }
            }

            foreach (var evaluatableCodeCell in codeCellsToEvaluate) {
                evaluatableCodeCell.View.Reset ();
                evaluatableCodeCell.View.IsEvaluating = true;

                switch (await CoreEvaluateCodeCellAsync (evaluatableCodeCell)) {
                case CodeCellEvaluationStatus.ErrorDiagnostic:
                case CodeCellEvaluationStatus.Disconnected:
                    return;
                }
            }

            if (isLastCell && !evaluateAll)
                StartNewCodeCell ();

            // NOTE: I cannot remember why this has to be run after awaiting
            // CoreEvaluateCodeCellAsync but it does... so don't move it? -abock
            if (clientSession.ViewControllers.ReplHistory != null) {
                clientSession.ViewControllers.ReplHistory.CursorToEnd ();
                clientSession.ViewControllers.ReplHistory.Append (null);
            }
        }

        enum CodeCellEvaluationStatus
        {
            Success,
            Disconnected,
            Interrupted,
            ErrorDiagnostic,
            EvaluationException
        }

        async Task<CodeCellEvaluationStatus> CoreEvaluateCodeCellAsync (
            CodeCellState codeCellState,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            cancellationToken = clientSession.CancellationToken.LinkWith (cancellationToken);

            if (!clientSession.Agent.IsConnected || clientSession.CompilationWorkspace == null) {
                codeCellState.View.IsEvaluating = false;
                codeCellState.View.HasErrorDiagnostics = true;
                codeCellState.View.RenderDiagnostic (new InteractiveDiagnostic (
                    DiagnosticSeverity.Error,
                    "Cannot evaluate: not connected to agent."));
                return CodeCellEvaluationStatus.Disconnected;
            }

            CodeAnalysis.Compilation compilation = null;
            ExceptionNode exception = null;
            bool agentTerminatedWhileEvaluating = false;

            try {
                compilation = await clientSession.CompilationWorkspace.GetSubmissionCompilationAsync (
                    codeCellState.DocumentId,
                    new EvaluationEnvironment (clientSession.WorkingDirectory),
                    cancellationToken);

                var integrationAssemblies = compilation
                    .References
                    .Where (ra => ra.HasIntegration)
                    .ToArray ();
                if (integrationAssemblies.Length > 0)
                    await clientSession.Agent.Api.LoadAssembliesAsync (
                        clientSession.CompilationWorkspace.EvaluationContextId,
                        integrationAssemblies);

                foreach (var dependency in clientSession.CompilationWorkspace.WebDependencies) {
                    Guid guid;
                    if (clientSession.AddWebResource (dependency.Location, out guid))
                        await clientSession.LoadWorkbookDependencyAsync (
                            guid + dependency.Location.Extension);
                }
            } catch (Exception e) {
                exception = ExceptionNode.Create (e);
            }

            var diagnostics = clientSession.CompilationWorkspace.CurrentSubmissionDiagnostics.Filter ();
            codeCellState.View.HasErrorDiagnostics = diagnostics.HasErrors;

            foreach (var diagnostic in diagnostics)
                codeCellState.View.RenderDiagnostic (diagnostic);

            try {
                if (compilation != null) {
                    codeCellState.LastEvaluationRequestId = compilation.MessageId;
                    codeCellState.IsResultAnExpression = compilation.IsResultAnExpression;

                    await clientSession.Agent.Api.EvaluateAsync (
                        compilation,
                        cancellationToken);
                }
            } catch (XipErrorMessageException e) {
                exception = e.XipErrorMessage.Exception;
            } catch (Exception e) {
                Log.Error (TAG, "marking agent as terminated", e);
                agentTerminatedWhileEvaluating = true;
                codeCellState.View.HasErrorDiagnostics = true;
                codeCellState.View.RenderDiagnostic (new InteractiveDiagnostic (
                    DiagnosticSeverity.Error,
                    Catalog.GetString (
                        "The application terminated during evaluation of this cell. " +
                        "Run this cell manually to try again.")));
            }

            codeCellState.View.IsEvaluating = false;

            CodeCellEvaluationStatus evaluationStatus;

            if (exception != null) {
                codeCellState.View.RenderResult (
                    CultureInfo.CurrentCulture,
                    FilterException (exception),
                    EvaluationResultHandling.Replace);
                evaluationStatus = CodeCellEvaluationStatus.EvaluationException;
            } else if (diagnostics.HasErrors)
                return CodeCellEvaluationStatus.ErrorDiagnostic;
            else if (agentTerminatedWhileEvaluating)
                evaluationStatus = CodeCellEvaluationStatus.Disconnected;
            else
                evaluationStatus = CodeCellEvaluationStatus.Success;

            if (clientSession.SessionKind != ClientSessionKind.Workbook)
                codeCellState.Freeze ();

            codeCellState.NotifyEvaluated (agentTerminatedWhileEvaluating);
            return evaluationStatus;
        }

        public void RenderResult (Evaluation result)
        {
            if (result == null)
                return;

            var codeCellState = GetCodeCellStateById (result.CodeCellId);
            if (codeCellState == null)
                return;

            if (result.Result is RepresentedObject ro &&
                ro.Any (r => r is Guid guid && guid == EvaluationContextGlobalObject.clear)) {
                if (clientSession.SessionKind == ClientSessionKind.Workbook)
                    codeCellState.View.RenderDiagnostic (new InteractiveDiagnostic (
                        DiagnosticSeverity.Error,
                        "'clear' is not supported for Workbooks"));
                else
                    ClearAllCellViews ();

                return;
            }

            RenderResult (codeCellState, result, codeCellState.IsResultAnExpression);
        }

        static void RenderResult (
            CodeCellState codeCellState,
            Evaluation result,
            bool isResultAnExpression)
        {
            if (codeCellState == null)
                throw new ArgumentNullException (nameof (codeCellState));

            if (result == null)
                throw new ArgumentNullException (nameof (result));

            var cultureInfo = CultureInfo.CurrentCulture;
            try {
                cultureInfo = CultureInfo.GetCultureInfo (result.CultureLCID);
            } catch (Exception e) when (e is CultureNotFoundException ||
                e is ArgumentOutOfRangeException) {
                Log.Error (TAG, $"Invalid CultureInfo LCID: {result.CultureLCID}");
            }

            codeCellState.View.EvaluationDuration = result.EvaluationDuration;

            if (!result.Interrupted && result.Result != null || isResultAnExpression)
                codeCellState.View.RenderResult (
                        cultureInfo,
                        result.Result,
                        result.ResultHandling);
        }

        void ClearAllCellViews ()
            => workbookPage
                .Contents
                .OfType<CodeCell> ()
                .Select (codeCell => (CellView)codeCell.View)
                .ForEach (RemoveCellView);

        void RemoveCellView (CellView cellView)
        {
            if (cellView?.Editor == null || cellView.Editor.IsDisposed)
                return;

            cellView.Editor.Dispose ();
            clientSession.Workbook.EditorHub.RemoveEditor (cellView.Editor);

            if (cellView?.RootElement?.ParentElement != null)
                cellView.RootElement.ParentElement.RemoveChild (cellView.RootElement);
        }

        /// <summary>
		/// Dicards the captured traces and frames that are a result of compiler-generated
		/// code to host the submission so we only render frames the user might actually expect.
		/// </summary>
        static ExceptionNode FilterException (ExceptionNode exception)
        {
            try {
                var capturedTraces = exception?.StackTrace?.CapturedTraces;
                if (capturedTraces == null || capturedTraces.Count != 2)
                    return exception;

                var submissionTrace = capturedTraces [0];
                exception.StackTrace = exception.StackTrace.WithCapturedTraces (new [] {
                    submissionTrace.WithFrames (
                        submissionTrace.Frames.Take (submissionTrace.Frames.Count - 1))
                });

                return exception;
            } catch (Exception e) {
                Log.Error (TAG, $"error filtering ExceptionNode [[{exception}]]", e);
                return exception;
            }
        }

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

        CodeCellState GetCodeCellStateById (CodeCellId codeCellId)
        {
            var documentId = codeCellId.ToDocumentId ();
            return codeCells.Values.FirstOrDefault (
                codeCell => codeCell.DocumentId == documentId);
        }

        public void RenderCapturedOutputSegment (CapturedOutputSegment segment)
            => GetCodeCellStateById (segment.Context)?.View?.RenderCapturedOutputSegment (segment);

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
            var previousCellView = clientSession.Workbook.EditorHub.FocusedEditorState?.PreviousCell?.View;
            if (previousCellView == null)
                return false;

            previousCellView.Focus ();
            previousCellView.Editor.SetCursorPosition (AbstractCursorPosition.DocumentEnd);
            return true;
        }

        bool FocusNextCell ()
        {
            var nextCellView = clientSession.Workbook.EditorHub?.FocusedEditorState?.NextCell?.View;
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
                clientSession.ViewControllers.ReplHistory != null &&
                (evnt.NavigatePrevious ? ReplHistoryPrevious () : ReplHistoryNext ());

        bool ReplHistoryPrevious ()
        {
            if (!clientSession.ViewControllers.ReplHistory.IsPreviousAvailable)
                return false;

            var cell = focusedCellState?.Cell;
            if (cell == null)
                return false;
            
            clientSession.ViewControllers.ReplHistory.Update (cell.Buffer.Value);
            cell.Buffer.Value = clientSession.ViewControllers.ReplHistory.Previous ();

            return true;
        }

        bool ReplHistoryNext ()
        {
            if (!clientSession.ViewControllers.ReplHistory.IsNextAvailable)
                return false;

            var cell = focusedCellState?.Cell;
            if (cell == null)
                return false;
            
            clientSession.ViewControllers.ReplHistory.Update (cell.Buffer.Value);
            cell.Buffer.Value = clientSession.ViewControllers.ReplHistory.Next ();

            return true;
        }

        #endregion
    }
}