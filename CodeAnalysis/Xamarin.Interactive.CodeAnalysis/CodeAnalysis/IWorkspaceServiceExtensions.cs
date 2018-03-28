// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive.CodeAnalysis
{
    public static class IWorkspaceServiceExtensions
    {
        /// <summary>
        /// Inserts a new cell into the workspace and sets an initial buffer.
        /// The workspace implementation may not need both <see cref="CodeCellId"/>
        /// parameters, but they will always be provided, should they exist, by
        /// <see cref="EvaluationService"/>.
        /// </summary>
        /// <param name="initialBuffer">The initial the text content for the cell.</param>
        /// <param name="previousCellId">Insert the new cell immediately after this cell.</param>
        /// <param name="nextCellId">Insert the new cell immediately before this cell.</param>
        /// <returns>
        /// Returns the <see cref="CodeCellId"/> of the new cell. The ID must be
        /// unique, opaque, and persistent across the cell's lifecycle.
        /// </returns>
        public static CodeCellId InsertCell (
            this IWorkspaceService workspaceService,
            string initialBuffer,
            CodeCellId previousCellId,
            CodeCellId nextCellId)
        {
            var codeCellId = workspaceService.InsertCell (previousCellId, nextCellId);
            workspaceService.SetCellBuffer (codeCellId, initialBuffer);
            return codeCellId;
        }
    }
}