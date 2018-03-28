//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Models;
using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    /// <summary>
    /// Provides compilation and editor services (e.g. "IntelliSense" features)
    /// for a given language and target environment (e.g. against Xamarin.iOS or
    /// .NET Core, but not necessarily limited to the .NET ecosystem of languages).
    ///
    /// Does not actually perform any evaluation and is not intended to be hosted
    /// inside a remote agent. In the Roslyn case, C# is cross-compiled on a
    /// desktop (macOS, Windows) host into CIL and evaluated remotely in the target
    /// environment.
    /// </summary>
    public interface IWorkspaceService
    {
        /// <summary>
        /// Any context and configuration data that will be determinated before
        /// instantiating this workspace. It will also be provided by
        /// <see cref="IWorkspaceServiceActivator.CreateNew"/>, and should simply
        /// be passed through and set on this instance during construction.
        /// </summary>
        WorkspaceConfiguration Configuration { get; }

        /// <summary>
        /// Gets the list of all <see cref="CodeCellId"/> in this workspace in an
        /// order sensible for evaluation (a topologically sorted dependency graph).
        /// This is typically "top-down" in a Workbooks UX, but there can be nuances
        /// regarding cell dependencies that might not be exposed to the user.
        /// </summary>
        /// <returns>
        /// Returns a list of <see cref="CodeCellId"/>. Implementations should never
        /// return <c>null</c>.
        /// </returns>
        IReadOnlyList<CodeCellId> GetTopologicallySortedCellIds ();

        /// <summary>
        /// Inserts a new cell into the workspace. The workspace implementation may
        /// not need both <see cref="CodeCellId"/> parameters, but they will always
        /// be provided, should they exist, by <see cref="EvaluationService"/>.
        /// </summary>
        /// <param name="previousCellId">Insert the new cell immediately after this cell.</param>
        /// <param name="nextCellId">Insert the new cell immediately before this cell.</param>
        /// <returns>
        /// Returns the <see cref="CodeCellId"/> of the new cell. The ID must be
        /// unique, opaque, and persistent across the cell's lifecycle.
        /// </returns>
        CodeCellId InsertCell (CodeCellId previousCellId, CodeCellId nextCellId);

        /// <summary>
        /// Removes a cell from the workspace. The workspace implementation may
        /// not need both <see cref="CodeCellId"/> parameters, but they will always
        /// be provided, should they exist, by <see cref="EvaluationService"/>.
        /// </summary>
        /// <param name="cellId">The target cell ID to remove from the workspace.</param>
        /// <param name="nextCellId">
        /// The cell ID of the cell that immediately follows <paramref name="cellId"/>
        /// in the workspace before removal, if any.
        /// </param>
        void RemoveCell (CodeCellId cellId, CodeCellId nextCellId);

        /// <summary>
        /// Gets text content as a string of the cell identified by <paramref name="cellId"/>.
        /// <summary>
        /// <returns>
        /// Returns the text content as a string. Implementations should never return <c>null</c>.
        /// </returns>
        string GetCellBuffer (CodeCellId cellId);

        /// <summary>
        /// Updates the text content of the cell identified by <paramref name="cellId"/>
        /// to that of the value in <paramref name="buffer"/>. Implementers may store
        /// the buffer internally as something other than a string.
        /// </summary>
        void SetCellBuffer (CodeCellId cellId, string buffer);

        /// <summary>
        /// Determines, either syntactically, semantically, or otherwise, if the cell
        /// is a candidate for evaluation. This method is meant as a hint to a UX and
        /// will not necessarily inhibit evaluation of a cell.
        ///
        /// In a REPL scenario for example, when a user presses the enter key, this
        /// method determines if a secondary continuation prompt is presented to allow
        /// the user to continue providing input, or to evaluate it.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the cell may be evaluated, <c>false</c> if
        /// evaluation is not recommended as it will likely fail.
        /// </returns>
        bool IsCellComplete (CodeCellId cellId);

        /// <summary>
        /// Determines whether or not the cell identified by <paramref name="cellId"/>
        /// must be re-evaluated due to a change that, for example,
        /// <see cref="EvaluationService"/> may be unaware of.
        ///
        /// Scenarios for which implementers should return <c>true</c> include when
        /// assemblies referenced via <c>#r</c> or script files via <c>#load</c> have
        /// changed on disk.
        ///
        /// Similar to <see cref="IsCellComplete"/> but actually influences the evaluation
        /// model computed by <see cref="EvaluationService.GetEvaluationModelAsync"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the cell must be re-evaluated, <c>false</c> otherwise.
        /// </returns>
        bool IsCellOutdated (CodeCellId cellId);

        /// <summary>
        /// Gets a list of all current diagnostics for the cell identified by
        /// <paramref name="cellId"/>.
        ///
        /// The implementation of this method should do as little work as possible.
        /// Expect it to be called after each call to <see cref="SetCellBuffer"/>,
        /// which _could_ be as often as every key press.
        /// </summary>
        /// <returns>
        /// Returns a list of diagnostics. Implementations should never return <c>null</c>.
        /// </returns>
        Task<IReadOnlyList<Diagnostic>> GetCellDiagnosticsAsync (
            CodeCellId cellId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a complete compilation and emission of the cell identified by
        /// <paramref name="cellId"/>.
        ///
        /// For .NET languages, the Portable Executable image produced by Roslyn
        /// (for example) will be present on the returned <see cref="Compilation"/>,
        /// against which <see cref="System.Reflection.Assembly.Load(byte[])"/>
        /// will be called in order to start evaluation in the remote agent.
        /// </summary>
        /// <returns>
        /// Returns a populated <see cref="Compilation"/> object. Implementations
        /// should never return <c>null</c>, nor should they throw an exception on
        /// compilation failure. If compilation fails, the returned object should
        /// indicate such, and the caller should then call
        /// <see cref="GetCellDiagnosticsAsync"/>.
        /// </returns>
        Task<Compilation> EmitCellCompilationAsync (
            CodeCellId cellId,
            IEvaluationEnvironment evaluationEnvironment,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of code completion options for the code at a given
        /// <paramref name="position"/>.
        ///
        /// The implementation of this method should do as little work as possible.
        /// Expect it to be called after each call to <see cref="SetCellBuffer"/>,
        /// which _could_ be as often as every key press.
        /// </summary>
        /// <returns>
        /// Returns a list of completions to present to the user. May return either
        /// an empty enumerable or <c>null</c> to indicate there is nothing to present.
        /// </returns>
        Task<IEnumerable<CompletionItem>> GetCompletionsAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a hover tooltip to display in the editor for the code at a given
        /// <paramref name="position"/>. This method may be called frequently, likely
        /// after some short timeout when the mouse cursor hovers over the editor
        /// bound to the cell identified by <paramref name="cellId"/>.
        /// </summary>
        /// <returns>
        /// Returns the hover tooltip to display or <c>null</c> if there isn't one.
        /// </returns>
        Task<Hover> GetHoverAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets signature help data to display for the code at a given
        /// <paramref name="position"/>.
        ///
        /// The implementation of this method should do as little work as possible.
        /// Expect it to be called after each call to <see cref="SetCellBuffer"/>,
        /// which _could_ be as often as every key press.
        /// </summary>
        /// <returns>
        /// Returns the signature help to display or <c>null</c> if there isn't any.
        /// </returns>
        Task<SignatureHelp> GetSignatureHelpAsync (
            CodeCellId cellId,
            Position position,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of external dependencies (e.g. files) that this workspace
        /// knows about. This is used to collect and package resources that belong
        /// to a workbook/project/etc. Implementations would return paths/URIs to
        /// <c>#load</c> scripts and <c>#r</c> references for example.
        /// </summary>
        /// <returns>
        /// Returns a list of dependencies. May return either an empty list or
        /// <c>null</c> to indicate there are none.
        /// </returns>
        IEnumerable<ExternalDependency> GetExternalDependencies ();
    }
}