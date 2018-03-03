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

using Microsoft.CodeAnalysis;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.Compilation.Roslyn;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class EvaluationService : IEvaluationService
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

        readonly Inhibitor evaluationInhibitor = new Inhibitor ();

        readonly RoslynCompilationWorkspace workspace;
        readonly EvaluationEnvironment evaluationEnvironment;
        readonly IAgentConnection agentConnection;

        readonly Dictionary<CodeCellId, CodeCellState> cellStates
            = new Dictionary<CodeCellId, CodeCellState> ();

        CodeCellState nugetReferenceCellState;

        public EvaluationContextId Id => workspace.EvaluationContextId;

        readonly Observable<ICodeCellEvent> events = new Observable<ICodeCellEvent> ();
        public IObservable<ICodeCellEvent> Events => events;

        public bool CanEvaluate => !evaluationInhibitor.IsInhibited;

        public EvaluationService (
            RoslynCompilationWorkspace workspace,
            EvaluationEnvironment evaluationEnvironment,
            IAgentConnection agentConnection)
        {
            this.workspace = workspace
                ?? throw new ArgumentNullException (nameof (workspace));

            this.evaluationEnvironment = evaluationEnvironment;
            this.agentConnection = agentConnection;

            this.agentConnection.Api.Messages.Subscribe (new Observer<object> (OnAgentMessage));
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

            if (nugetReferenceCellState == null) {
                var firstCodeCellId = workspace
                    .GetTopologicallySortedSubmissionIds ()
                    .Select (CodeCellIdExtensions.ToCodeCellId)
                    .FirstOrDefault ();

                nugetReferenceCellState = await InsertCodeCellAsync (
                    string.Empty,
                    firstCodeCellId,
                    true,
                    cancellationToken);
            }

            // TODO: Prevent dupes. Return false if no changes made
            var builder = new StringBuilder (nugetReferenceCellState.Buffer.Value);
            foreach (var reference in references) {
                if (builder.Length > 0)
                    //builder.AppendLine ();
                    builder.Append ("\n");
                builder
                    .Append ("#r \"")
                    .Append (reference)
                    .Append ("\"");
            }

            nugetReferenceCellState.Buffer.Value = builder.ToString ();
            return true;
        }

        #endregion

        #region Agent Messages

        void OnAgentMessage (object message)
        {
            if (message is Evaluation evaluation)
                events.Observers.OnNext (new CodeCellResultEvent (
                    evaluation.CodeCellId,
                    evaluation.ResultHandling,
                    (RepresentedObject)evaluation.Result));
        }

        #endregion

        ImmutableList<CodeCellId> GetTopologicallySortedCodeCellIds ()
            => workspace
                .GetTopologicallySortedSubmissionIds ()
                .Select (CodeCellIdExtensions.ToCodeCellId)
                .ToImmutableList ();

        public Task<IReadOnlyList<CodeCellState>> GetAllCodeCellsAsync (
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CodeCellState>> (
                GetTopologicallySortedCodeCellIds ()
                .Select (id => cellStates [id])
                .ToList ());

        public Task<CodeCellState> InsertCodeCellAsync (
            string initialBuffer = null,
            CodeCellId relativeToCodeCellId = default,
            bool insertBefore = false,
            CancellationToken cancellationToken = default)
        {
            var buffer = new CodeCellBuffer ();
            if (!string.IsNullOrEmpty (initialBuffer))
                buffer.Value = initialBuffer;

            var cells = GetTopologicallySortedCodeCellIds ();
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

            var codeCellId = workspace.AddSubmission (
                buffer.CurrentText,
                previousCodeCellId.ToDocumentId (),
                nextCodeCellId.ToDocumentId ()).ToCodeCellId ();

            var codeCellState = new CodeCellState (
                codeCellId,
                buffer);

            cellStates.Add (codeCellId, codeCellState);

            return Task.FromResult (codeCellState);
        }

        public Task<CodeCellState> UpdateCodeCellAsync (
            CodeCellId codeCellId,
            string buffer,
            CancellationToken cancellationToken = default)
        {
            var cell = cellStates [codeCellId];
            cell.IsDirty = true;
            cell.Buffer.Value = buffer;
            return Task.FromResult (cell);
        }

        public Task RemoveCodeCellAsync (
            CodeCellId codeCellId,
            CancellationToken cancellationToken = default)
        {
            var cells = GetTopologicallySortedCodeCellIds ();
            var index = cells.IndexOf (codeCellId);
            var nextCodeCellId = index < cells.Count - 1
                ? cells [index + 1]
                : default;

            workspace.RemoveSubmission (
                codeCellId.ToDocumentId (),
                nextCodeCellId.ToDocumentId ());
            
            cellStates.Remove (codeCellId);

            return Task.CompletedTask;
        }

        public async Task EvaluateAsync (
            CodeCellId targetCodeCellId = default,
            bool evaluateAll = false,
            CancellationToken cancellationToken = default)
        {
            var evaluationModel = await GetEvaluationModelAsync (
                targetCodeCellId,
                evaluateAll,
                cancellationToken);

            if (evaluationModel.ShouldResetAgentState)
                await agentConnection.Api.ResetStateAsync ();

            foreach (var evaluatableCodeCell in evaluationModel.CellsToEvaluate) {
                evaluatableCodeCell.Reset ();
                evaluatableCodeCell.IsEvaluating = true;

                switch (await CoreEvaluateCodeCellAsync (evaluatableCodeCell)) {
                    case CodeCellEvaluationStatus.ErrorDiagnostic:
                    case CodeCellEvaluationStatus.Disconnected:
                        return;
                }
            }
        }

        internal sealed class EvaluationModel
        {
            public bool ShouldResetAgentState { get; set; }
            public List<CodeCellState> CellsToEvaluate { get; } = new List<CodeCellState> ();
        }

        internal async Task<EvaluationModel> GetEvaluationModelAsync (
            CodeCellId targetCodeCellId = default,
            bool evaluateAll = false,
            CancellationToken cancellationToken = default)
        {
            var model = new EvaluationModel ();
            var cells = await GetAllCodeCellsAsync ();
            var haveSeenTargetCell = false;
            var skipRemainingCells = false;

            for (int i = 0; i < cells.Count; i++) {
                var cell = cells [i];
                var isTargetCell = cell.Id == targetCodeCellId;
                var shouldEvaluate = isTargetCell || cell.IsEvaluationCandidate;
                haveSeenTargetCell |= isTargetCell;

                if (!shouldEvaluate && workspace.HaveAnyLoadDirectiveFilesChanged (cell.Id.ToDocumentId ())) {
                    cell.Buffer.Invalidate ();
                    shouldEvaluate = true;
                }

                if (shouldEvaluate) {
                    if (i == 0)
                        model.ShouldResetAgentState = true;

                    model.CellsToEvaluate.Add (cell);
                }

                if (skipRemainingCells || cell.AgentTerminatedWhileEvaluating)
                    skipRemainingCells = true;
                else if (evaluateAll || cell.EvaluationCount > 0)
                    model.CellsToEvaluate.Add (cell);
                
                cell.IsOutdated = true;
            }

            return model;
        }

        async Task<CodeCellEvaluationStatus> CoreEvaluateCodeCellAsync (
            CodeCellState codeCellState,
            CancellationToken cancellationToken = default)
        {
            if (!agentConnection.IsConnected) {
                codeCellState.AppendDiagnostic (new InteractiveDiagnostic (
                    DiagnosticSeverity.Error,
                    "Cannot evaluate: not connected to agent."));
                return CodeCellEvaluationStatus.Disconnected;
            }

            Compilation compilation = null;
            ExceptionNode exception = null;
            bool agentTerminatedWhileEvaluating = false;

            try {
                compilation = await workspace.GetSubmissionCompilationAsync (
                    codeCellState.Id.ToDocumentId (),
                    evaluationEnvironment,
                    cancellationToken);

                var integrationAssemblies = compilation
                    .References
                    .Where (ra => ra.HasIntegration)
                    .ToArray ();

                if (integrationAssemblies.Length > 0)
                    await agentConnection.Api.LoadAssembliesAsync (
                        workspace.EvaluationContextId,
                        integrationAssemblies);

                // FIXME: this is where we'd LoadWorkbookDependencyAsync
            } catch (Exception e) {
                exception = ExceptionNode.Create (e);
            }

            var diagnostics = workspace
                .CurrentSubmissionDiagnostics
                .Filter ();

            foreach (var diagnostic in diagnostics)
                codeCellState.AppendDiagnostic ((InteractiveDiagnostic)diagnostic);

            try {
                if (compilation != null) {
                    codeCellState.LastEvaluationRequestId = compilation.MessageId;
                    codeCellState.IsResultAnExpression = compilation.IsResultAnExpression;

                    await agentConnection.Api.EvaluateAsync (
                        compilation,
                        cancellationToken);
                }
            } catch (XipErrorMessageException e) {
                exception = e.XipErrorMessage.Exception;
            } catch (Exception e) {
                Log.Error (TAG, "marking agent as terminated", e);
                agentTerminatedWhileEvaluating = true;
                codeCellState.AppendDiagnostic (new InteractiveDiagnostic (
                    DiagnosticSeverity.Error,
                    Catalog.GetString (
                        "The application terminated during evaluation of this cell. " +
                        "Run this cell manually to try again.")));
            }

            codeCellState.IsEvaluating = false;

            CodeCellEvaluationStatus evaluationStatus;

            if (exception != null) {
                events.Observers.OnNext (new CodeCellResultEvent (
                    codeCellState.Id,
                    EvaluationResultHandling.Replace,
                    FilterException (exception)));
                evaluationStatus = CodeCellEvaluationStatus.EvaluationException;
            } else if (diagnostics.HasErrors) {
                return CodeCellEvaluationStatus.ErrorDiagnostic;
            } else if (agentTerminatedWhileEvaluating) {
                evaluationStatus = CodeCellEvaluationStatus.Disconnected;
            } else {
                evaluationStatus = CodeCellEvaluationStatus.Success;
            }

            codeCellState.NotifyEvaluated (agentTerminatedWhileEvaluating);

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