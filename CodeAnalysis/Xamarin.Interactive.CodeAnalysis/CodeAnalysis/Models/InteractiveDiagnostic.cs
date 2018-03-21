// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    [InteractiveSerializable ("evaluation.Diagnostic")]
    public struct InteractiveDiagnostic
    {
        public PositionSpan Span { get; }
        public InteractiveDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string Id { get; }

        [JsonConstructor]
        public InteractiveDiagnostic (
            PositionSpan span,
            InteractiveDiagnosticSeverity severity,
            string message,
            string id)
        {
            Span = span;
            Severity = severity;
            Message = message;
            Id = id;
        }

        public InteractiveDiagnostic (InteractiveDiagnosticSeverity severity, string message)
            : this (default, severity, message, null)
        {
        }
    }
}