// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Models;

using InteractiveDiagnostic = Xamarin.Interactive.CodeAnalysis.Models.Diagnostic;
using InteractiveDiagnosticSeverity = Xamarin.Interactive.CodeAnalysis.Models.DiagnosticSeverity;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    static class ConversionExtensions
    {
        public static LinePosition ToRoslyn (this Position position)
            => new LinePosition (
                position.LineNumber - 1,
                position.Column - 1);

        public static Position FromRoslyn (this LinePosition position)
            => new Position (
                position.Line + 1,
                position.Character + 1);

        public static LinePositionSpan ToRoslyn (this Range range)
        {
            if (range.StartLineNumber < 1 || range.StartColumn < 1)
                return default;

            var start = new LinePosition (
                range.StartLineNumber - 1,
                range.StartColumn - 1);

            if (range.EndLineNumber < 1 || range.EndColumn < 1)
                return new LinePositionSpan (start, start);

            return new LinePositionSpan (
                start,
                new LinePosition (
                    range.EndLineNumber - 1,
                    range.EndColumn - 1));
        }

        public static Range FromRoslyn (this LinePositionSpan span)
            => new Range (
                span.Start.Line + 1,
                span.Start.Character + 1,
                span.End.Line + 1,
                span.End.Character + 1);

        public static Range FromRoslyn (this Location location)
        {
            var span = location.GetMappedLineSpan ();
            if (!span.IsValid)
                span = location.GetLineSpan ();

            return new Range (
                span.StartLinePosition.Line + 1,
                span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1,
                span.EndLinePosition.Character + 1,
                span.Path);
        }

        public static CodeCellId ToCodeCellId (this DocumentId documentId)
            => new CodeCellId (documentId.ProjectId.Id, documentId.Id);

        public static DocumentId ToDocumentId (this CodeCellId codeCellId)
        {
            if (codeCellId == default)
                return default;

            return DocumentId.CreateFromSerialized (
                ProjectId.CreateFromSerialized (codeCellId.ProjectId),
                codeCellId.Id);
        }

        public static InteractiveDiagnostic ToInteractiveDiagnostic (
            this Microsoft.CodeAnalysis.Diagnostic diagnostic)
            => new InteractiveDiagnostic (
                diagnostic.Location.FromRoslyn (),
                (InteractiveDiagnosticSeverity)(int)diagnostic.Severity,
                diagnostic.GetMessage (),
                diagnostic.Id);

        public static CompletionItemKind ToCompletionItemKind (ImmutableArray<string> completionTags)
        {
            if (completionTags.Length == 0)
                goto @default;

            if (completionTags.Contains (CompletionTags.Assembly) ||
                completionTags.Contains (CompletionTags.Namespace) ||
                completionTags.Contains (CompletionTags.Project) ||
                completionTags.Contains (CompletionTags.Module))
                return CompletionItemKind.Module;

            if (completionTags.Contains (CompletionTags.Class) ||
                completionTags.Contains (CompletionTags.Structure))
                return CompletionItemKind.Class;

            if (completionTags.Contains (CompletionTags.Constant) ||
                completionTags.Contains (CompletionTags.Field) ||
                completionTags.Contains (CompletionTags.Delegate) ||
                completionTags.Contains (CompletionTags.Event) ||
                completionTags.Contains (CompletionTags.Local))
                return CompletionItemKind.Field;

            if (completionTags.Contains (CompletionTags.Enum) ||
                completionTags.Contains (CompletionTags.EnumMember))
                return CompletionItemKind.Enum;

            if (completionTags.Contains (CompletionTags.Method) ||
                completionTags.Contains (CompletionTags.Operator))
                return CompletionItemKind.Method;

            if (completionTags.Contains (CompletionTags.ExtensionMethod))
                return CompletionItemKind.Function;

            if (completionTags.Contains (CompletionTags.Interface))
                return CompletionItemKind.Interface;

            if (completionTags.Contains (CompletionTags.Property))
                return CompletionItemKind.Property;

            if (completionTags.Contains (CompletionTags.Keyword))
                return CompletionItemKind.Keyword;

            if (completionTags.Contains (CompletionTags.Reference))
                return CompletionItemKind.Reference;

            if (completionTags.Contains (CompletionTags.Snippet))
                return CompletionItemKind.Snippet;

        @default:
            return CompletionItemKind.Text;
        }
    }
}