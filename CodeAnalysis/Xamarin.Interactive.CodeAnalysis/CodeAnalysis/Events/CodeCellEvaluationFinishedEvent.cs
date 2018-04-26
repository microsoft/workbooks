//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    public struct CodeCellEvaluationFinishedEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public EvaluationStatus Status { get; }
        public bool ShouldStartNewCell { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public CodeCellEvaluationFinishedEvent (
            CodeCellId codeCellId,
            EvaluationStatus status,
            bool shouldStartNewCell,
            IReadOnlyList<Diagnostic> diagnostics)
        {
            CodeCellId = codeCellId;
            Status = status;
            ShouldStartNewCell = shouldStartNewCell;
            Diagnostics = diagnostics;
        }
    }
}