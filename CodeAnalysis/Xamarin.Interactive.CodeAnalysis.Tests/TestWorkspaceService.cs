// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.CodeAnalysis.Resolving;

[assembly: Xamarin.Interactive.CodeAnalysis.WorkspaceService (
    "testlang",
    typeof (Xamarin.Interactive.CodeAnalysis.TestWorkspaceService.Activator))]

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Intended as a base class for unit testing.
    /// </summary>
    public class TestWorkspaceService : IWorkspaceService
    {
        public sealed class Activator : IWorkspaceServiceActivator
        {
            public Task<IWorkspaceService> CreateNew (
                LanguageDescription languageDescription,
                WorkspaceConfiguration configuration,
                CancellationToken cancellationToken)
                => Task.FromResult<IWorkspaceService> (new TestWorkspaceService (configuration));
        }

        public WorkspaceConfiguration Configuration { get; }

        public TestWorkspaceService (WorkspaceConfiguration configuration)
            => Configuration = configuration;

        public virtual IReadOnlyList<CodeCellId> GetTopologicallySortedCellIds ()
            => throw new NotImplementedException ();

        public virtual CodeCellId InsertCell (
            CodeCellId previousCellId,
            CodeCellId nextCellId)
            => throw new NotImplementedException ();

        public virtual void RemoveCell (CodeCellId cellId, CodeCellId nextCellId)
            => throw new NotImplementedException ();

        public virtual Task<string> GetCellBufferAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public virtual void SetCellBuffer (CodeCellId cellId, string buffer)
            => throw new NotImplementedException ();

        public bool IsCellComplete (CodeCellId cellId)
            => throw new NotImplementedException ();

        public Task<bool> IsCellOutdatedAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task<IReadOnlyList<Diagnostic>> GetCellDiagnosticsAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public virtual Task<Compilation> EmitCellCompilationAsync (
            CodeCellId cellId,
            EvaluationEnvironment evaluationEnvironment,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        #region IntelliNonsense

        public Task<IEnumerable<CompletionItem>> GetCompletionsAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task<Hover> GetHoverAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public Task<SignatureHelp> GetSignatureHelpAsync (
            CodeCellId cellId,
            Position position, CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        #endregion

        public IEnumerable<ExternalDependency> GetExternalDependencies ()
            => throw new NotImplementedException ();
    }
}