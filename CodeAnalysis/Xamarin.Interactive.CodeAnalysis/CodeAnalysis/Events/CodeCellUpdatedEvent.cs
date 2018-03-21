//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.CodeAnalysis.Events
{
    public struct CodeCellUpdatedEvent : ICodeCellEvent
    {
        public CodeCellId CodeCellId { get; }
        public bool IsSubmissionComplete { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public CodeCellUpdatedEvent (
            CodeCellId codeCellId,
            bool isSubmissionComplete,
            IReadOnlyList<Diagnostic> diagnostics)
        {
            CodeCellId = codeCellId;
            IsSubmissionComplete = isSubmissionComplete;
            Diagnostics = diagnostics;
        }
    }
}