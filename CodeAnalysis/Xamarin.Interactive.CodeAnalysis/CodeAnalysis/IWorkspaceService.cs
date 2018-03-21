//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IWorkspaceService
    {
        EvaluationContextId EvaluationContextId { get; }

        WorkspaceConfiguration Configuration { get; }

        ImmutableList<CodeCellId> GetTopologicallySortedCellIds ();

        CodeCellId InsertCell (
            string initialBuffer,
            CodeCellId previousCellId,
            CodeCellId nextCellId);

        void RemoveCell (CodeCellId cellId, CodeCellId nextCellId);

        bool IsCellComplete (CodeCellId cellId);

        void SetCellBuffer (CodeCellId cellId, string buffer);

        string GetCellBuffer (CodeCellId cellId);

        bool IsCellOutdated (CodeCellId cellId);

        Task<ImmutableList<InteractiveDiagnostic>> GetCellDiagnosticsAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default);

        // FIXME: extend Compilation with Diagnostics, but this will
        // require moving more into XI that currently lives in XIC
        // -abock, 2018-03-08
        Task<(Compilation compilation, ImmutableList<InteractiveDiagnostic> diagnostics)> GetCellCompilationAsync (
            CodeCellId cellId,
            IEvaluationEnvironment evaluationEnvironment,
            CancellationToken cancellationToken = default);

        Task<Hover> GetHoverAsync (
            CodeCellId codeCellId,
            Position position,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<CompletionItem>> GetCompletionsAsync (
            CodeCellId codeCellId,
            Position position,
            CancellationToken cancellationToken = default);

        Task<SignatureHelp> GetSignatureHelpAsync (
            CodeCellId codeCellId,
            Position position,
            CancellationToken cancellationToken = default);
    }
}