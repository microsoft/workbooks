// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Models;

namespace Xamarin.Interactive.Compilation.Roslyn
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

        public static LinePositionSpan ToRoslyn (this PositionSpan span)
            => new LinePositionSpan (
                new LinePosition (
                    span.StartLineNumber - 1,
                    span.StartColumn - 1),
                new LinePosition (
                    span.EndLineNumber - 1,
                    span.EndColumn - 1));

        public static PositionSpan FromRoslyn (this LinePositionSpan span)
            => new PositionSpan (
                span.Start.Line + 1,
                span.Start.Character + 1,
                span.End.Line + 1,
                span.End.Character + 1);

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

        public static InteractiveDiagnostic ToInteractiveDiagnostic (this Diagnostic diagnostic)
            => new InteractiveDiagnostic (
                PositionSpan.FromRoslyn (diagnostic.Location),
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