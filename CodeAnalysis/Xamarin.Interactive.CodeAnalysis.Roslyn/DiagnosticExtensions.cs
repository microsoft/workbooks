//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    static class DiagnosticExtensions
    {
        public static FilteredDiagnostics Filter (
            this IReadOnlyList<Diagnostic> diagnostics,
            int maxCount = 5)
            => new FilteredDiagnostics (diagnostics, maxCount);
    }
}