//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.Editor;
using Xamarin.Interactive.Editor.Events;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Workbook.Views;

namespace Xamarin.Interactive.Workbook.Models
{
    abstract class WorkbookPageViewModel : IObserver<ClientSessionEvent>, IEvaluationService
    {
        const string TAG = nameof (WorkbookPageViewModel);

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

        readonly Inhibitor evaluationInhibitor = new Inhibitor ();

        ImmutableDictionary<IEditor, CodeCellState> codeCells = ImmutableDictionary<IEditor, CodeCellState>.Empty;
        protected IReadOnlyDictionary<IEditor, CodeCellState> CodeCells => codeCells;

        protected ClientSession ClientSession { get; }
        protected WorkbookPage WorkbookPage { get; }

        protected WorkbookPageViewModel (ClientSession clientSession, WorkbookPage workbookPage)
        {
            ClientSession = clientSession
                ?? throw new ArgumentNullException (nameof (clientSession));

            WorkbookPage = workbookPage
                ?? throw new ArgumentNullException (nameof (workbookPage));
        }

        #region Public API

        public void LoadWorkbookPage ()
        {
            LoadWorkbookCells ();

            if (WorkbookPage.Contents.GetFirstCell<CodeCell> () == null)
                StartNewCodeCell ();

            WorkbookPage
                ?.Contents
                ?.GetFirstCell<CodeCell> ()
                ?.View
                ?.Focus ();
        }

        public void Dispose ()
        {
            GC.SuppressFinalize (this);
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
        }

        public IDisposable InhibitEvaluate () => evaluationInhibitor.Inhibit ();

        public bool CanEvaluate => !evaluationInhibitor.IsInhibited;

        public virtual Task LoadWorkbookDependencyAsync (
            string dependency,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        #endregion

        #region Workbook Structure

        protected abstract void BindMarkdownCellToView (MarkdownCell cell);

        protected abstract void BindCodeCellToView (CodeCell cell, CodeCellState codeCellState);

        protected abstract void UnbindCellFromView (ICellView cellView);

        protected abstract void InsertCellInViewModel (Cell newCell, Cell previousCell);

        void AppendCodeCell (IEditor editor, CodeCellState codeCellState)
            => codeCells = codeCells.Add (editor, codeCellState);

        void RemoveCodeCell (IEditor editor)
            => codeCells = codeCells.Remove (editor);

        void ClearAllCellViews ()
            => WorkbookPage
                .Contents
                .OfType<CodeCell> ()
                .Select (codeCell => codeCell.View)
                .ForEach (UnbindCellFromView);

        DocumentId GetDocumentId (CodeCell codeCell)
        {
            if (codeCell?.View?.Editor != null &&
                CodeCells.TryGetValue (codeCell.View.Editor, out CodeCellState codeCellState))
                return codeCellState.DocumentId;
            return null;
        }

        void PopulateCompilationWorkspace ()
        {
            DocumentId previousDocumentId = null;

            foreach (var codeCell in WorkbookPage.Contents.OfType<CodeCell> ()) {
                var editor = codeCell?.View?.Editor;
                if (editor == null || !CodeCells.TryGetValue (editor, out var codeCellState))
                    continue;

                codeCellState.CompilationWorkspace = ClientSession.CompilationWorkspace;
                codeCellState.DocumentId = ClientSession.CompilationWorkspace.AddSubmission (
                    codeCell.CodeAnalysisBuffer.CurrentText,
                    previousDocumentId,
                    null);

                previousDocumentId = codeCellState.DocumentId;
            }
        }

        void LoadWorkbookCells ()
        {
            foreach (var cell in WorkbookPage.Contents) {
                switch (cell) {
                case CodeCell codeCell:
                    InsertCodeCell (codeCell, codeCell.PreviousCell);
                    break;
                case MarkdownCell markdownCell:
                    InsertMarkdownCell (markdownCell, markdownCell.PreviousCell);
                    break;
                }
            }
        }

        protected CodeCellState InsertCodeCell (Cell previousCell)
            => InsertCodeCell (new CodeCell ("csharp"), previousCell);

        class HiddenCodeCellView : ICodeCellView
        {
            public IEditor Editor { get; set; }
            public bool IsOutdated { get; set; }
            public bool IsDirty { get; set; }
            public bool IsEvaluating { get; set; }

            public bool IsFrozen { get; private set; }

            public bool HasErrorDiagnostics { get; set; }
            public TimeSpan EvaluationDuration { get; set; }

            public void Focus (bool scrollIntoView = true)
            {
            }

            public void Freeze ()
                => IsFrozen = true;

            public void RenderCapturedOutputSegment (CapturedOutputSegment segment)
            {
            }

            public void RenderDiagnostic (InteractiveDiagnostic diagnostic)
            {
            }

            public void RenderResult (CultureInfo cultureInfo, object result, EvaluationResultHandling resultHandling)
            {
            }

            public void Reset ()
            {
            }
        }

        class HiddenCodeCellEditor : IEditor
        {
            public IObservable<EditorEvent> Events => throw new NotImplementedException ();

            public bool IsDisposed { get; private set; }

            public void Dispose ()
                => IsDisposed = true;

            public void ExecuteCommand (EditorCommand command)
            {
                throw new NotImplementedException ();
            }

            public void Focus ()
            {
                throw new NotImplementedException ();
            }

            public IEnumerable<EditorCommand> GetCommands ()
            {
                throw new NotImplementedException ();
            }

            public EditorCommandStatus GetCommandStatus (EditorCommand command)
            {
                throw new NotImplementedException ();
            }

            public void OnBlur ()
            {
                throw new NotImplementedException ();
            }

            public void SetCursorPosition (AbstractCursorPosition cursorPosition)
            {
                throw new NotImplementedException ();
            }

            public bool TryGetCommand (string commandId, out EditorCommand command)
            {
                throw new NotImplementedException ();
            }
        }

        CodeCellState InsertHiddenCell ()
            => InsertCodeCell (
                new CodeCell ("csharp", shouldSerialize: false),
                WorkbookPage.Contents.FirstCell.GetSelfOrNextCell<CodeCell> ()?.PreviousCell,
                isHidden: true);

        CodeCellState InsertCodeCell (CodeCell newCell, Cell previousCell, bool isHidden = false)
        {
            if (newCell == null)
                throw new ArgumentNullException (nameof (newCell));

            InsertCellInDocumentModel (newCell, previousCell);

            var previousCodeCell = newCell.GetPreviousCell<CodeCell> ();
            var nextCodeCell = newCell.GetNextCell<CodeCell> ();

            var codeCellState = new CodeCellState (newCell);

            if (isHidden) {
                // Set up editor, required as dictionary key
                codeCellState.Editor = new HiddenCodeCellEditor ();
                codeCellState.View = new HiddenCodeCellView { Editor = codeCellState.Editor };
                newCell.View = codeCellState.View;
            } else
                BindCodeCellToView (newCell, codeCellState);

            if (ClientSession.CompilationWorkspace != null) {
                codeCellState.CompilationWorkspace = ClientSession.CompilationWorkspace;
                codeCellState.DocumentId = ClientSession.CompilationWorkspace.AddSubmission (
                    newCell.CodeAnalysisBuffer.CurrentText,
                    GetDocumentId (previousCodeCell),
                    GetDocumentId (nextCodeCell));
            }

            if (!isHidden)
                InsertCellInViewModel (newCell, previousCell);

            OutdateAllCodeCells (newCell);

            AppendCodeCell (codeCellState.Editor, codeCellState);

            return codeCellState;
        }

        protected void InsertOrFocusMarkdownCell (Cell previousCell)
        {
            if (previousCell?.NextCell is MarkdownCell nextMarkdownCell)
                nextMarkdownCell.View.Focus ();
            else
                InsertMarkdownCell (previousCell);
        }

        protected void InsertMarkdownCell (Cell previousCell)
            => InsertMarkdownCell (new MarkdownCell (), previousCell);

        void InsertMarkdownCell (MarkdownCell newCell, Cell previousCell)
        {
            InsertCellInDocumentModel (newCell, previousCell);

            BindMarkdownCellToView (newCell);

            InsertCellInViewModel (newCell, previousCell);
        }

        void InsertCellInDocumentModel (Cell newCell, Cell previousCell)
        {
            if (newCell.Document != null)
                return;

            if (previousCell == null && WorkbookPage.Contents.FirstCell == null)
                WorkbookPage.Contents.AppendCell (newCell);
            else if (previousCell == null)
                WorkbookPage.Contents.InsertCellBefore (
                    WorkbookPage.Contents.FirstCell,
                    newCell);
            else
                WorkbookPage.Contents.InsertCellAfter (previousCell, newCell);
        }

        protected void DeleteCell (Cell cell)
        {
            if (cell == null)
                throw new ArgumentNullException (nameof (cell));

            if (cell is CodeCell codeCell) {
                OutdateAllCodeCells (codeCell);

                if (ClientSession.CompilationWorkspace != null)
                    ClientSession.CompilationWorkspace.RemoveSubmission (
                        GetDocumentId (codeCell),
                        GetDocumentId (codeCell.GetNextCell<CodeCell> ()));

                RemoveCodeCell (cell.View.Editor);
            }

            var focusCell = cell.NextCell ?? cell.PreviousCell;

            cell.Document.RemoveCell (cell);

            UnbindCellFromView (cell.View);

            focusCell?.View?.Focus ();
        }

        public void OutdateAllCodeCells ()
            => OutdateAllCodeCells (WorkbookPage.Contents.GetFirstCell<CodeCell> ());

        void OutdateAllCodeCells (CodeCell codeCell)
        {
            while (codeCell != null) {
                var view = (ICodeCellView)codeCell.View;
                if (view != null)
                    view.IsOutdated = true;
                codeCell = codeCell.GetNextCell<CodeCell> ();
            }
        }

        protected virtual CodeCellState StartNewCodeCell ()
            => InsertCodeCell (
                new CodeCell ("csharp"),
                WorkbookPage?.Contents?.LastCell);

        #endregion

        #region IObserver<ClientSessionEvent>

        void IObserver<ClientSessionEvent>.OnNext (ClientSessionEvent evnt)
        {
            switch (evnt.Kind) {
            case ClientSessionEventKind.AgentConnected:
                OnAgentConnected ();
                break;
            case ClientSessionEventKind.AgentDisconnected:
                OnAgentDisconnected ();
                break;
            case ClientSessionEventKind.CompilationWorkspaceAvailable:
                PopulateCompilationWorkspace ();
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

        protected virtual void OnAgentConnected ()
        {
            ClientSession.Agent.Api.Messages.Subscribe (new Observer<object> (HandleAgentMessage));

            void HandleAgentMessage (object message)
            {
                if (message is CapturedOutputSegment segment)
                    MainThread.Post (() => RenderCapturedOutputSegment (segment));

                if (message is Evaluation result)
                    MainThread.Post (() => RenderResult (result));
            }
        }

        protected virtual void OnAgentDisconnected ()
        {
        }

        protected virtual void OnCompilationWorkspaceAvailable ()
        {
        }

        #endregion

        #region Evaluation

        protected async Task AbortEvaluationAsync ()
        {
            if (!ClientSession.Agent.IsConnected)
                return;

            await ClientSession.Agent.Api.AbortEvaluationAsync (
                ClientSession.CompilationWorkspace.EvaluationContextId);
        }

        public Task<bool> AddTopLevelReferencesAsync (
            IReadOnlyList<string> references,
            CancellationToken cancellationToken = default)
        {
            // TODO: soo.....why are new #r's added after there are other cells not bringing in the reference right?
            if (references == null || references.Count == 0)
                return Task.FromResult (false);

            // TODO: Should we be saving a quick reference to the hidden cell/editor?
            var hiddenCellState = CodeCells
                .Where (p => p.Key is HiddenCodeCellEditor)
                .Select (p => p.Value)
                .FirstOrDefault ();

            if (hiddenCellState == null)
                hiddenCellState = InsertHiddenCell ();

            // TODO: Prevent dupes. Return false if no changes made
            var builder = new StringBuilder (hiddenCellState.Cell.Buffer.Value);
            foreach (var reference in references) {
                if (builder.Length > 0)
                    //builder.AppendLine ();
                    builder.Append ("\n");
                builder
                    .Append ("#r \"")
                    .Append (reference)
                    .Append ("\"");
            }

            hiddenCellState.Cell.Buffer.Value = builder.ToString ();
            return Task.FromResult (true);
        }

        public async Task EvaluateAllAsync (CancellationToken cancellationToken = default)
        {
            var firstCell = WorkbookPage.Contents.GetFirstCell<CodeCell> ();
            if (firstCell?.View?.Editor == null)
                return;

            if (CodeCells.TryGetValue (firstCell.View.Editor, out var codeCellState))
                await EvaluateCodeCellAsync (codeCellState, evaluateAll: true);
        }

        public async Task EvaluateAsync (
            string input,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (!CanEvaluate || string.IsNullOrWhiteSpace (input))
                return;

            using (InhibitEvaluate ()) {
                if (!CodeCells.TryGetValue (
                    WorkbookPage?.Contents?.GetLastCell<CodeCell> ()?.View?.Editor,
                    out var codeCell) ||
                    codeCell.Cell.Buffer.Length > 0)
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

        protected async Task EvaluateCodeCellAsync (CodeCellState codeCellState, bool evaluateAll = false)
        {
            if (!CanEvaluate || codeCellState.IsFrozen)
                return;

            using (InhibitEvaluate ())
                await DoEvaluateCodeCellAsync (codeCellState, evaluateAll);
        }

        // Only call this from EvaluateCodeCellAsync, for CanEvaluate handling
        async Task DoEvaluateCodeCellAsync (CodeCellState codeCellState, bool evaluateAll = false)
        {
            await ClientSession.EnsureAgentConnectionAsync ();

            var codeCellsToEvaluate = ImmutableList<CodeCellState>.Empty;
            var originalCodeCellState = codeCellState;

            if (ClientSession.ViewControllers.ReplHistory != null) {
                ClientSession.ViewControllers.ReplHistory.UpdateLastAppended (
                    codeCellState.Cell.Buffer.Value.Trim ());
                ClientSession.ViewControllers.ReplHistory.Save ();
            }

            var codeCell = originalCodeCellState.Cell;
            var isLastCell = codeCell.GetNextCell<CodeCell> () == null;
            var isFirstCell = codeCell.GetPreviousCell<CodeCell> () == null;

            if (isFirstCell && ClientSession.SessionKind == ClientSessionKind.Workbook)
                await ClientSession.Agent.Api.ResetStateAsync ();

            while (codeCell != null) {
                if (CodeCells.TryGetValue (codeCell.View.Editor, out codeCellState)) {
                    var evaluateCodeCell =
                        codeCellState == originalCodeCellState ||
                        codeCellState.EvaluationCount == 0 ||
                                     codeCellState.View.IsDirty ||
                                     codeCellState.View.IsOutdated;

                    if (ClientSession.CompilationWorkspace.HaveAnyLoadDirectiveFilesChanged (
                        codeCellState.DocumentId)) {
                        codeCellState.Cell.CodeAnalysisBuffer.Invalidate ();
                        evaluateCodeCell = true;
                    }

                    if (evaluateCodeCell)
                        codeCellsToEvaluate = codeCellsToEvaluate.Insert (0, codeCellState);
                }

                codeCell = codeCell.GetPreviousCell<CodeCell> ();
            }

            codeCell = originalCodeCellState.Cell;
            var skipRemainingCodeCells = false;
            while (true) {
                codeCell = codeCell.GetNextCell<CodeCell> ();
                if (codeCell == null)
                    break;

                if (CodeCells.TryGetValue (codeCell.View.Editor, out codeCellState)) {
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
            if (ClientSession.ViewControllers.ReplHistory != null) {
                ClientSession.ViewControllers.ReplHistory.CursorToEnd ();
                ClientSession.ViewControllers.ReplHistory.Append (null);
            }
        }

        async Task<CodeCellEvaluationStatus> CoreEvaluateCodeCellAsync (
            CodeCellState codeCellState,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            cancellationToken = ClientSession.CancellationToken.LinkWith (cancellationToken);

            if (!ClientSession.Agent.IsConnected || ClientSession.CompilationWorkspace == null) {
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
                compilation = await ClientSession.CompilationWorkspace.GetSubmissionCompilationAsync (
                    codeCellState.DocumentId,
                    new EvaluationEnvironment (ClientSession.WorkingDirectory),
                    cancellationToken);

                var integrationAssemblies = compilation
                    .References
                    .Where (ra => ra.HasIntegration)
                    .ToArray ();
                if (integrationAssemblies.Length > 0)
                    await ClientSession.Agent.Api.LoadAssembliesAsync (
                        ClientSession.CompilationWorkspace.EvaluationContextId,
                        integrationAssemblies);

                foreach (var dependency in ClientSession.CompilationWorkspace.WebDependencies) {
                    if (ClientSession.AddWebResource (dependency.Location, out var guid))
                        await LoadWorkbookDependencyAsync (guid + dependency.Location.Extension);
                }
            } catch (Exception e) {
                exception = ExceptionNode.Create (e);
            }

            var diagnostics = ClientSession.CompilationWorkspace.CurrentSubmissionDiagnostics.Filter ();
            codeCellState.View.HasErrorDiagnostics = diagnostics.HasErrors;

            foreach (var diagnostic in diagnostics)
                codeCellState.View.RenderDiagnostic ((InteractiveDiagnostic)diagnostic);

            try {
                if (compilation != null) {
                    codeCellState.LastEvaluationRequestId = compilation.MessageId;
                    codeCellState.IsResultAnExpression = compilation.IsResultAnExpression;

                    await ClientSession.Agent.Api.EvaluateAsync (
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
                    EvaluationService.FilterException (exception),
                    EvaluationResultHandling.Replace);
                evaluationStatus = CodeCellEvaluationStatus.EvaluationException;
            } else if (diagnostics.HasErrors) {
                return CodeCellEvaluationStatus.ErrorDiagnostic;
            } else if (agentTerminatedWhileEvaluating) {
                evaluationStatus = CodeCellEvaluationStatus.Disconnected;
            } else {
                evaluationStatus = CodeCellEvaluationStatus.Success;
            }

            if (ClientSession.SessionKind != ClientSessionKind.Workbook)
                codeCellState.Freeze ();

            codeCellState.NotifyEvaluated (agentTerminatedWhileEvaluating);
            return evaluationStatus;
        }

        #endregion

        #region Evaluation Result Handling

        CodeCellState GetCodeCellStateById (CodeCellId codeCellId)
        {
            var documentId = codeCellId.ToDocumentId ();
            return CodeCells.Values.FirstOrDefault (
                codeCell => codeCell.DocumentId == documentId);
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
            } catch (Exception e) when (
                e is CultureNotFoundException ||
                e is ArgumentOutOfRangeException) {
                Log.Error (TAG, $"Invalid CultureInfo LCID: {result.CultureLCID}");
            }

            codeCellState.View.EvaluationDuration = result.EvaluationDuration;

            if (result.Exception != null)
                codeCellState.View.RenderResult (
                    cultureInfo,
                    EvaluationService.FilterException (result.Exception),
                    result.ResultHandling);
            else if (!result.Interrupted && result.Result != null || isResultAnExpression)
                codeCellState.View.RenderResult (
                    cultureInfo,
                    result.Result,
                    result.ResultHandling);
        }

        void RenderResult (Evaluation result)
        {
            if (result == null)
                return;

            var codeCellState = GetCodeCellStateById (result.CodeCellId);
            if (codeCellState == null)
                return;

            if (result.Result is RepresentedObject ro &&
                ro.Any (r => r is Guid guid && guid == EvaluationContextGlobalObject.clear)) {
                if (ClientSession.SessionKind == ClientSessionKind.Workbook)
                    codeCellState.View.RenderDiagnostic (new InteractiveDiagnostic (
                        DiagnosticSeverity.Error,
                        "'clear' is not supported for Workbooks"));
                else
                    ClearAllCellViews ();

                return;
            }

            RenderResult (codeCellState, result, codeCellState.IsResultAnExpression);
        }

        void RenderCapturedOutputSegment (CapturedOutputSegment segment)
            => GetCodeCellStateById (segment.Context)?.View?.RenderCapturedOutputSegment (segment);

        #endregion
    }
}