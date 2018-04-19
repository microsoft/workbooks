//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    public sealed class EvaluationService : IEvaluationService
    {
        const string TAG = nameof (EvaluationService);

        sealed class Inhibitor : IDisposable
        {
            int inhibitions;

            public bool IsInhibited => inhibitions > 0;

            public IDisposable Inhibit ()
            {
                inhibitions++;
                return this;
            }

            public void Dispose ()
                => inhibitions--;
        }

        internal sealed class CodeCellState
        {
            public CodeCellId CodeCellId { get; }

            public IReadOnlyList<Diagnostic> Diagnostics { get; set; }
                = Array.Empty<Diagnostic> ();

            public bool IsDirty { get; set; }
            public bool AgentTerminatedWhileEvaluating { get; set; }
            public int EvaluationCount { get; set; }

            public CodeCellState (CodeCellId codeCellId)
                => CodeCellId = codeCellId;

            public bool IsEvaluationCandidate => IsDirty || EvaluationCount == 0;
        }

        internal sealed class EvaluationModel
        {
            public bool ShouldResetAgentState { get; set; }
            public bool ShouldMaybeStartNewCodeCell { get; set; }
            public List<CodeCellState> CellsToEvaluate { get; } = new List<CodeCellState> ();
        }

        readonly object stateChangeLock = new object ();
        readonly Inhibitor evaluationInhibitor = new Inhibitor ();

        readonly IWorkspaceService workspace;

        EvaluationEnvironment evaluationEnvironment;
        IEvaluationContextManager evaluationContextManager;
        IDisposable evaluationContextManagerEventsSubscription;

        readonly Dictionary<CodeCellId, CodeCellState> cellStates
            = new Dictionary<CodeCellId, CodeCellState> ();

        CodeCellId nugetReferenceCellId;

        public TargetCompilationConfiguration TargetCompilationConfiguration => workspace
            .Configuration
            .CompilationConfiguration;

        readonly Observable<ICodeCellEvent> events = new Observable<ICodeCellEvent> ();
        public IObservable<ICodeCellEvent> Events => events;

        public bool CanEvaluate => !evaluationInhibitor.IsInhibited;

        public EvaluationService (
            IWorkspaceService workspace,
            EvaluationEnvironment evaluationEnvironment)
        {
            this.workspace = workspace
                ?? throw new ArgumentNullException (nameof (workspace));

            this.evaluationEnvironment = evaluationEnvironment;
        }

        internal void NotifyEvaluationEnvironmentChanged (EvaluationEnvironment evaluationEnvironment)
        {
            lock (stateChangeLock)
                this.evaluationEnvironment = evaluationEnvironment;
        }

        internal void NotifyPeerUpdated (IEvaluationContextManager evaluationContextManager)
        {
            lock (stateChangeLock) {
                evaluationContextManagerEventsSubscription?.Dispose ();
                evaluationContextManagerEventsSubscription = null;

                this.evaluationContextManager = evaluationContextManager;

                evaluationContextManagerEventsSubscription = evaluationContextManager
                    ?.Events
                    ?.Subscribe (events.Observers);
            }
        }

        #region IEvaluationService

        public void Dispose ()
        {
            evaluationInhibitor.Dispose ();
        }

        public void OutdateAllCodeCells ()
        {
        }

        public IDisposable InhibitEvaluate ()
            => evaluationInhibitor.Inhibit ();

        public Task EvaluateAsync (string input, CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task LoadWorkbookDependencyAsync (string dependency, CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task EvaluateAllAsync (CancellationToken cancellationToken = default)
            => EvaluateAsync (evaluateAll: true, cancellationToken: cancellationToken);

        public async Task<bool> AddTopLevelReferencesAsync (
            IReadOnlyList<string> references,
            CancellationToken cancellationToken = default)
        {
            if (references == null || references.Count == 0)
                return false;

            var buffer = new StringBuilder ();

            if (nugetReferenceCellId == default) {
                var firstCodeCellId = workspace
                    .GetTopologicallySortedCellIds ()
                    .FirstOrDefault ();

                nugetReferenceCellId = await InsertCodeCellAsync (
                    string.Empty,
                    firstCodeCellId,
                    true,
                    cancellationToken);
            } else {
                buffer.Append (await workspace.GetCellBufferAsync (
                    nugetReferenceCellId,
                    cancellationToken));
            }

            // TODO: Prevent dupes. Return false if no changes made
            foreach (var reference in references) {
                if (buffer.Length > 0)
                    buffer.Append ("\n");

                buffer
                    .Append ("#r \"")
                    .Append (reference)
                    .Append ("\"");
            }

            workspace.SetCellBuffer (nugetReferenceCellId, buffer.ToString ());

            return true;
        }

        #endregion

        internal Task<ImmutableList<CodeCellState>> GetAllCodeCellsAsync (
            CancellationToken cancellationToken = default)
            => Task.FromResult (workspace
                .GetTopologicallySortedCellIds ()
                .Select (id => cellStates [id])
                .ToImmutableList ());

        public Task<CodeCellId> InsertCodeCellAsync (
            string initialBuffer = null,
            CodeCellId relativeToCodeCellId = default,
            bool insertBefore = false,
            CancellationToken cancellationToken = default)
        {
            var cells = workspace.GetTopologicallySortedCellIds ();
            var insertionIndex = cells.Count;

            if (relativeToCodeCellId != CodeCellId.Empty) {
                insertionIndex = cells.FindIndex (id => id == relativeToCodeCellId);
                if (insertionIndex < 0)
                    throw new ArgumentException (
                        $"CodeCellId not found: {relativeToCodeCellId}",
                        nameof (relativeToCodeCellId));

                if (!insertBefore)
                    insertionIndex++;
            }

            var previousCodeCellId = CodeCellId.Empty;
            var nextCodeCellId = CodeCellId.Empty;

            if (insertionIndex > 0)
                previousCodeCellId = cells [insertionIndex - 1];

            if (insertionIndex < cells.Count)
                nextCodeCellId = cells [insertionIndex];

            var codeCellId = workspace.InsertCell (
                previousCodeCellId,
                nextCodeCellId);

            workspace.SetCellBuffer (
                codeCellId,
                initialBuffer);

            var codeCellState = new CodeCellState (
                codeCellId);

            cellStates.Add (codeCellId, codeCellState);

            return Task.FromResult (codeCellId);
        }

        public async Task<CodeCellUpdatedEvent> UpdateCodeCellAsync (
            CodeCellId codeCellId,
            string buffer,
            CancellationToken cancellationToken = default)
        {
            workspace.SetCellBuffer (codeCellId, buffer);

            var cell = cellStates [codeCellId];
            cell.IsDirty = true;

            return new CodeCellUpdatedEvent (
                cell.CodeCellId,
                workspace.IsCellComplete (cell.CodeCellId),
                await workspace.GetCellDiagnosticsAsync (
                    cell.CodeCellId,
                    cancellationToken));
        }

        public Task RemoveCodeCellAsync (
            CodeCellId codeCellId,
            CancellationToken cancellationToken = default)
        {
            var cells = workspace.GetTopologicallySortedCellIds ();
            var index = cells.IndexOf (codeCellId);
            var nextCodeCellId = index < cells.Count - 1
                ? cells [index + 1]
                : default;

            workspace.RemoveCell (
                codeCellId,
                nextCodeCellId);

            cellStates.Remove (codeCellId);

            return Task.CompletedTask;
        }

        internal async Task<EvaluationModel> GetEvaluationModelAsync (
            CodeCellId targetCodeCellId = default,
            bool evaluateAll = false,
            CancellationToken cancellationToken = default)
        {
            var model = new EvaluationModel ();
            var cells = await GetAllCodeCellsAsync ();

            var targetCellIndex = evaluateAll
                ? -1
                : cells.FindIndex (cell => cell.CodeCellId == targetCodeCellId);

            // we're either evaluating all cells head to tail or we failed to
            // find the target cell in the list; either way we're bailing early
            if (targetCellIndex < 0) {
                if (!evaluateAll && targetCodeCellId != default)
                    throw new KeyNotFoundException (
                        $"{nameof (targetCodeCellId)} '{targetCodeCellId}' not found");

                model.ShouldResetAgentState = true;
                model.CellsToEvaluate.AddRange (cells);
                return model;
            }

            // otherwise, starting with our target cell and working backwards to the,
            // head of the cell list figure out cells that need to be evaluated.
            for (var i = targetCellIndex; i >= 0; i--) {
                var cell = cells [i];

                var isTargetCell = targetCellIndex == i;
                var shouldEvaluate = isTargetCell || cell.IsEvaluationCandidate;

                if (!shouldEvaluate && await workspace.IsCellOutdatedAsync (cell.CodeCellId, cancellationToken))
                    shouldEvaluate = true;

                if (shouldEvaluate) {
                    model.ShouldResetAgentState |= i == 0;
                    model.CellsToEvaluate.Insert (0, cell);
                }
            }

            // now look at all cells after our target cell; if any of them have been
            // evaluated before, we also want to re-evaluate those since they may
            // depend on state in previous cells which will become invalidated.
            for (var i = targetCellIndex + 1; i < cells.Count; i++) {
                var cell = cells [i];

                // if a cell was previously run but resulted in an agent termination,
                // we do not want to automatically re-run that cell; the user must
                // explicitly re-run terminated cells (which would be handled in the
                // target->head walk above).
                if (cell.AgentTerminatedWhileEvaluating)
                    break;

                // otherwise if we've evaluated this cell before, we should do so again
                if (cell.EvaluationCount > 0)
                    model.CellsToEvaluate.Add (cell);

                // FIXME: this is where we did codeCellState.View.IsOutdated = true;
                // but I do not know why we did that yet. Let this be a clue to future
                // self. -abock, 2018-03-07
            }

            model.ShouldMaybeStartNewCodeCell = targetCellIndex == cells.Count - 1;

            return model;
        }

        public async Task<CodeCellEvaluationFinishedEvent> EvaluateAsync (
            CodeCellId targetCodeCellId = default,
            bool evaluateAll = false,
            CancellationToken cancellationToken = default)
        {
            var evaluationModel = await GetEvaluationModelAsync (
                targetCodeCellId,
                evaluateAll,
                cancellationToken);

            if (evaluationModel.ShouldResetAgentState)
                await evaluationContextManager.ResetStateAsync (
                    TargetCompilationConfiguration.EvaluationContextId,
                    cancellationToken);

            CodeCellEvaluationFinishedEvent lastCellFinishedEvent = default;

            foreach (var evaluatableCodeCell in evaluationModel.CellsToEvaluate) {
                events.Observers.OnNext (
                    new CodeCellEvaluationStartedEvent (
                        evaluatableCodeCell.CodeCellId));

                var status = await CoreEvaluateCodeCellAsync (evaluatableCodeCell);

                lastCellFinishedEvent = new CodeCellEvaluationFinishedEvent (
                    evaluatableCodeCell.CodeCellId,
                    status,
                    evaluationModel.ShouldMaybeStartNewCodeCell &&
                    evaluatableCodeCell.CodeCellId == targetCodeCellId,
                    evaluatableCodeCell.Diagnostics);

                events.Observers.OnNext (lastCellFinishedEvent);

                switch (status) {
                case CodeCellEvaluationStatus.ErrorDiagnostic:
                case CodeCellEvaluationStatus.Disconnected:
                    return lastCellFinishedEvent;
                }
            }

            return lastCellFinishedEvent;
        }

        async Task<CodeCellEvaluationStatus> CoreEvaluateCodeCellAsync (
            CodeCellState codeCellState,
            CancellationToken cancellationToken = default)
        {
            if (evaluationContextManager == null) {
                codeCellState.Diagnostics = new [] {
                    new Diagnostic (
                        DiagnosticSeverity.Error,
                        "Cannot evaluate: not connected to agent.")
                };
                return CodeCellEvaluationStatus.Disconnected;
            }

            Compilation compilation = null;
            IReadOnlyList<Diagnostic> diagnostics = null;
            ExceptionNode exception = null;

            try {
                compilation = await workspace.EmitCellCompilationAsync (
                    codeCellState.CodeCellId,
                    evaluationEnvironment,
                    cancellationToken);

                diagnostics = await workspace.GetCellDiagnosticsAsync (
                    codeCellState.CodeCellId,
                    cancellationToken);

                var integrationAssemblies = compilation
                    .References
                    .Where (ra => ra.HasIntegration)
                    .ToArray ();

                if (integrationAssemblies.Length > 0)
                    await evaluationContextManager.LoadAssembliesAsync (
                        TargetCompilationConfiguration.EvaluationContextId,
                        integrationAssemblies,
                        cancellationToken);

                // FIXME: this is where we'd LoadWorkbookDependencyAsync
            } catch (Exception e) {
                exception = ExceptionNode.Create (e);
            }

            codeCellState.Diagnostics = diagnostics;

            try {
                if (compilation != null)
                    await evaluationContextManager.EvaluateAsync (
                        TargetCompilationConfiguration.EvaluationContextId,
                        compilation,
                        cancellationToken);
            } catch (XipErrorMessageException e) {
                exception = e.XipErrorMessage.Exception;
            } catch (Exception e) {
                Log.Error (TAG, "marking agent as terminated", e);
                codeCellState.AgentTerminatedWhileEvaluating = true;
                codeCellState.Diagnostics = new [] {
                    new Diagnostic (
                        DiagnosticSeverity.Error,
                        Catalog.GetString (
                            "The application terminated during evaluation of this cell. " +
                            "Run this cell manually to try again."))
                };
            }

            CodeCellEvaluationStatus evaluationStatus;

            if (exception != null) {
                events.Observers.OnNext (new Evaluation (
                    codeCellState.CodeCellId,
                    EvaluationResultHandling.Replace,
                    FilterException (exception)));
                evaluationStatus = CodeCellEvaluationStatus.EvaluationException;
            } else if (diagnostics.Any (d => d.Severity == DiagnosticSeverity.Error)) {
                return CodeCellEvaluationStatus.ErrorDiagnostic;
            } else if (codeCellState.AgentTerminatedWhileEvaluating) {
                evaluationStatus = CodeCellEvaluationStatus.Disconnected;
            } else {
                evaluationStatus = CodeCellEvaluationStatus.Success;
            }

            codeCellState.IsDirty = false;
            codeCellState.EvaluationCount++;

            return evaluationStatus;
        }

        /// <summary>
        /// Dicards the captured traces and frames that are a result of compiler-generated
        /// code to host the submission so we only render frames the user might actually expect.
        /// </summary>
        internal static ExceptionNode FilterException (ExceptionNode exception)
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
    }
}