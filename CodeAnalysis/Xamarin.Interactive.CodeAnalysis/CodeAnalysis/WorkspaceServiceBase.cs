// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    public abstract class WorkspaceServiceBase : IWorkspaceService
    {
        protected sealed class CellData
        {
            public CodeCellId Id { get; }
            public string Buffer { get; set; }

            public CellData (CodeCellId id)
                => Id = id;
        }

        readonly Guid workspaceId = Guid.NewGuid ();
        readonly LinkedList<CellData> cells = new LinkedList<CellData> ();

        LinkedListNode<CellData> FindCellNode (CodeCellId cellId, bool throwIfNotFound = false)
        {
            for (var node = cells.First; node != null; node = node.Next) {
                if (node.Value.Id == cellId)
                    return node;
            }

            if (throwIfNotFound)
                throw new ArgumentException ($"Cell {cellId} not found in workspace");

            return null;
        }

        protected CellData FindCell (CodeCellId cellId, bool throwIfNotFound = true)
            => FindCellNode (cellId, throwIfNotFound)?.Value;

        public WorkspaceConfiguration Configuration { get; }

        protected WorkspaceServiceBase (WorkspaceConfiguration configuration)
            => Configuration = configuration;

        public IReadOnlyList<CodeCellId> GetTopologicallySortedCellIds ()
            => cells.Select (data => data.Id).ToList ();

        public CodeCellId InsertCell (
            CodeCellId previousCellId,
            CodeCellId nextCellId)
        {
            var newCell = new CellData (new CodeCellId (workspaceId, Guid.NewGuid ()));
            LinkedListNode<CellData> newCellNode = null;

            if (previousCellId != default) {
                var previousCellNode = FindCellNode (previousCellId);
                if (previousCellNode != null)
                    newCellNode = cells.AddAfter (previousCellNode, newCell);
            } else if (nextCellId != default) {
                var nextCellNode = FindCellNode (nextCellId);
                if (nextCellId != null)
                    newCellNode = cells.AddBefore (nextCellNode, newCell);
            } else {
                newCellNode = cells.AddFirst (newCell);
            }

            if (newCellNode == null)
                throw new ArgumentException (
                    $"No cells in workspace for {nameof (previousCellId)}={previousCellId} " +
                    $"or {nameof (nextCellId)}={nextCellId}");

            OnCellInserted (newCell);

            return newCell.Id;
        }

        protected virtual void OnCellInserted (CellData cellData)
        {
        }

        public void RemoveCell (CodeCellId cellId, CodeCellId nextCellId)
        {
            var cellNode = FindCellNode (cellId, throwIfNotFound: true);
            cells.Remove (cellNode);
            OnCellRemoved (cellNode.Value);
        }

        protected virtual void OnCellRemoved (CellData cellData)
        {
        }

        public Task<string> GetCellBufferAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default)
        {
            var cellNode = FindCellNode (cellId, throwIfNotFound: true);
            return Task.FromResult (cellNode.Value.Buffer);
        }

        public void SetCellBuffer (CodeCellId cellId, string buffer)
        {
            var cellNode = FindCellNode (cellId, throwIfNotFound: true);
            cellNode.Value.Buffer = buffer;
            OnCellUpdated (cellNode.Value);
        }

        protected virtual void OnCellUpdated (CellData cellData)
        {
        }

        public bool IsCellComplete (CodeCellId cellId)
            => throw new NotImplementedException ();

        public virtual Task<bool> IsCellOutdatedAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public virtual Task<IReadOnlyList<Diagnostic>> GetCellDiagnosticsAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public virtual Task<Compilation> EmitCellCompilationAsync (
            CodeCellId cellId,
            EvaluationEnvironment evaluationEnvironment,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        #region IntelliNonsense

        public virtual Task<IEnumerable<CompletionItem>> GetCompletionsAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public virtual Task<Hover> GetHoverAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        public virtual Task<SignatureHelp> GetSignatureHelpAsync (
            CodeCellId cellId,
            Position position, CancellationToken cancellationToken = default)
            => throw new NotImplementedException ();

        #endregion

        public virtual IEnumerable<ExternalDependency> GetExternalDependencies ()
            => throw new NotImplementedException ();
    }
}