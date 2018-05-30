//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.NuGet;
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
        readonly PackageManagerService packageManager;

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
            PackageManagerService packageManager,
            EvaluationEnvironment evaluationEnvironment,
            IEvaluationContextManager evaluationContextManager = null)
        {
            this.workspace = workspace
                ?? throw new ArgumentNullException (nameof (workspace));

            this.packageManager = packageManager;
            if (packageManager != null)
                packageManager.PropertyChanged += HandlePackageManagerPropertyChanged;

            NotifyEvaluationEnvironmentChanged (evaluationEnvironment);
            NotifyEvaluationContextManagerChanged (evaluationContextManager);

            Events.Subscribe (new Observer<ICodeCellEvent> (OnCodeCellEvent));
        }

        void HandlePackageManagerPropertyChanged (object sender, PropertyChangedEventArgs e)
            => UpdatePackagesAsync (packageManager.InstalledPackages).Forget ();

        internal void NotifyEvaluationEnvironmentChanged (EvaluationEnvironment evaluationEnvironment)
        {
            lock (stateChangeLock)
                this.evaluationEnvironment = evaluationEnvironment;
        }

        public void NotifyEvaluationContextManagerChanged (IEvaluationContextManager evaluationContextManager)
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
            if (packageManager != null)
                packageManager.PropertyChanged -= HandlePackageManagerPropertyChanged;
        }

        public void OutdateAllCodeCells ()
        {
        }

        public IDisposable InhibitEvaluate ()
            => evaluationInhibitor.Inhibit ();

        Task IEvaluationService.EvaluateAsync (string input, CancellationToken cancellationToken)
            => throw new NotImplementedException ();

        Task IEvaluationService.LoadWorkbookDependencyAsync (string dependency, CancellationToken cancellationToken)
            => throw new NotImplementedException ();

        public Task EvaluateAllAsync (CancellationToken cancellationToken = default)
            => EvaluateAsync (evaluateAll: true, cancellationToken: cancellationToken);

        async Task UpdatePackagesAsync (
            IReadOnlyList<InteractivePackage> packages,
            CancellationToken cancellationToken = default)
        {
            if (packages == null || packages.Count == 0)
                return;

            if (nugetReferenceCellId == default) {
                var firstCodeCellId = workspace
                    .GetTopologicallySortedCellIds ()
                    .FirstOrDefault ();

                nugetReferenceCellId = await InsertCodeCellAsync (
                    string.Empty,
                    firstCodeCellId,
                    true,
                    cancellationToken);
            }

            var buffer = new StringBuilder ();

            foreach (var reference in packages.SelectMany (package => package.AssemblyReferences)) {
                if (buffer.Length > 0)
                    buffer.Append ("\n");

                buffer
                    .Append ("#r \"nugetref:")
                    .Append (reference)
                    .Append ("\"");
            }

            workspace.SetCellBuffer (nugetReferenceCellId, buffer.ToString ());

            var cell = cellStates [nugetReferenceCellId];
            cell.IsDirty = true;
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
                        status == EvaluationStatus.Success &&
                        evaluatableCodeCell.CodeCellId == targetCodeCellId,
                    evaluatableCodeCell.Diagnostics);

                events.Observers.OnNext (lastCellFinishedEvent);

                if (status != EvaluationStatus.Success)
                    NotifyEvaluationComplete (evaluatableCodeCell.CodeCellId, status);

                switch (status) {
                case EvaluationStatus.ErrorDiagnostic:
                case EvaluationStatus.Disconnected:
                    return lastCellFinishedEvent;
                }
            }

            return lastCellFinishedEvent;
        }

        readonly ConcurrentDictionary<CodeCellId, TaskCompletionSource<EvaluationStatus>> evaluationAwaiters
            = new ConcurrentDictionary<CodeCellId, TaskCompletionSource<EvaluationStatus>> ();

        void OnCodeCellEvent (ICodeCellEvent evnt)
        {
            if (evnt is Evaluation evaluation)
                NotifyEvaluationComplete (evaluation.CodeCellId, evaluation.Status);
        }

        public void NotifyEvaluationComplete (CodeCellId targetCodeCellId, EvaluationStatus status)
        {
            if (evaluationAwaiters.TryRemove (targetCodeCellId, out var awaiter))
                awaiter.SetResult (status);
        }

        async Task<EvaluationStatus> CoreEvaluateCodeCellAsync (
            CodeCellState codeCellState,
            CancellationToken cancellationToken = default)
        {
            if (evaluationContextManager == null) {
                codeCellState.Diagnostics = new [] {
                    new Diagnostic (
                        DiagnosticSeverity.Error,
                        "Cannot evaluate: not connected to agent.")
                };
                return EvaluationStatus.Disconnected;
            }

            var evaluationCompletion = new TaskCompletionSource<EvaluationStatus> ();
            if (!evaluationAwaiters.TryAdd (codeCellState.CodeCellId, evaluationCompletion))
                throw new Exception ($"This is already being evaluated: {codeCellState.CodeCellId}");

            Compilation compilation = null;

            compilation = await workspace.EmitCellCompilationAsync (
                codeCellState.CodeCellId,
                evaluationEnvironment,
                cancellationToken);

            codeCellState.Diagnostics = await workspace.GetCellDiagnosticsAsync (
                codeCellState.CodeCellId,
                cancellationToken);

            if (codeCellState.Diagnostics.Any (d => d.Severity == DiagnosticSeverity.Error))
                return EvaluationStatus.ErrorDiagnostic;

            var integrationAssemblies = compilation
                .References
                .Where (ra => ra.HasIntegration)
                .ToArray ();

            try {
                if (integrationAssemblies.Length > 0)
                    await evaluationContextManager.LoadAssembliesAsync (
                        TargetCompilationConfiguration.EvaluationContextId,
                        integrationAssemblies,
                        cancellationToken);

                await evaluationContextManager.EvaluateAsync (
                    TargetCompilationConfiguration.EvaluationContextId,
                    compilation,
                    cancellationToken);
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

            var evaluationStatus = await evaluationCompletion.Task;

            if (codeCellState.AgentTerminatedWhileEvaluating)
                evaluationStatus = EvaluationStatus.Disconnected;

            codeCellState.IsDirty = false;
            codeCellState.EvaluationCount++;

            return evaluationStatus;
        }
    }
}