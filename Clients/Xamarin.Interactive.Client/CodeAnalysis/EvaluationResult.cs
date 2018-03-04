//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.CodeAnalysis
{
    sealed class EvaluationResult
    {
        public bool Success { get; }
        public bool ShouldStartNewCell { get; }
        public IReadOnlyList<CodeCellState> CodeCellStates { get; }

        public EvaluationResult (
            bool success,
            bool shouldStartNewCell,
            IReadOnlyList<CodeCellState> codeCellStates)
        {
            Success = success;
            ShouldStartNewCell = shouldStartNewCell;
            CodeCellStates = codeCellStates
                ?? throw new ArgumentNullException (nameof (codeCellStates));
        }
    }
}