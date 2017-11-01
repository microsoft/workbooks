//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis
{
    struct InteractiveDiagnostic
    {
        public FileLinePositionSpan Span { get; }
        public DiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string Id { get; }

        public InteractiveDiagnostic (DiagnosticSeverity severity, string message)
        {
            Span = default (FileLinePositionSpan);
            Severity = severity;
            Message = message;
            Id = null;
        }

        public InteractiveDiagnostic (Diagnostic diagnostic)
        {
            Span = diagnostic.Location.GetMappedLineSpan ();
            if (!Span.IsValid)
                Span = diagnostic.Location.GetLineSpan ();

            Severity = diagnostic.Severity;
            Message = diagnostic.GetMessage ();
            Id = diagnostic.Id;
        }

        public static explicit operator InteractiveDiagnostic (Diagnostic diagnostic) =>
            new InteractiveDiagnostic (diagnostic);
    }
}