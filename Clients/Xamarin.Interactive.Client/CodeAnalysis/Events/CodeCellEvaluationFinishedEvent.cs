//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    struct CodeCellEvaluationFinishedEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public CodeCellEvaluationStatus Status { get; }
        public bool ShouldStartNewCell { get; }
        public IReadOnlyList<InteractiveDiagnostic> Diagnostics { get; }

        public CodeCellEvaluationFinishedEvent (
            CodeCellId codeCellId,
            CodeCellEvaluationStatus status,
            bool shouldStartNewCell,
            IReadOnlyList<InteractiveDiagnostic> diagnostics)
        {
            CodeCellId = codeCellId;
            Status = status;
            ShouldStartNewCell = shouldStartNewCell;
            Diagnostics = diagnostics;
        }
    }
}