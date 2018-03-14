//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    struct CodeCellUpdatedEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public bool IsSubmissionComplete { get; }
        public IReadOnlyList<InteractiveDiagnostic> Diagnostics { get; }

        public CodeCellUpdatedEvent (
            CodeCellId codeCellId,
            bool isSubmissionComplete,
            IReadOnlyList<InteractiveDiagnostic> diagnostics)
        {
            CodeCellId = codeCellId;
            IsSubmissionComplete = isSubmissionComplete;
            Diagnostics = diagnostics;
        }
    }
}