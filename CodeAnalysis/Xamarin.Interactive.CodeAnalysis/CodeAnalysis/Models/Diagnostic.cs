// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Xamarin.Interactive.CodeAnalysis.Models
{
    [InteractiveSerializable ("evaluation.Diagnostic")]
    public struct Diagnostic
    {
        public Range Range { get; }
        public DiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string Id { get; }

        [JsonConstructor]
        public Diagnostic (
            Range range,
            DiagnosticSeverity severity,
            string message,
            string id)
        {
            Range = range;
            Severity = severity;
            Message = message;
            Id = id;
        }

        public Diagnostic (DiagnosticSeverity severity, string message)
            : this (default, severity, message, null)
        {
        }
    }
}