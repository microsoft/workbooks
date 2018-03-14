//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Xamarin.Interactive.CodeAnalysis;

namespace Xamarin.Interactive.Compilation.Roslyn
{
    static class ConversionExtensions
    {
        public static LinePosition ToRoslyn (this Position position)
            => new LinePosition (position.LineNumber - 1, position.Column - 1);

        public static Position FromRoslyn (this LinePosition position)
            => new Position (position.Line + 1, position.Character + 1);

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
                diagnostic.Severity,
                diagnostic.GetMessage (),
                diagnostic.Id);
    }
}