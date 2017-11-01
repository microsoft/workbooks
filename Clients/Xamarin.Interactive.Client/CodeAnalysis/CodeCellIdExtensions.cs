//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Xamarin.Interactive.CodeAnalysis
{
    static class CodeCellIdExtensions
    {
        public static CodeCellId ToCodeCellId (this DocumentId documentId)
            => new CodeCellId (documentId.ProjectId.Id, documentId.Id);

        public static DocumentId ToDocumentId (this CodeCellId codeCellId)
            => DocumentId.CreateFromSerialized (
                ProjectId.CreateFromSerialized (codeCellId.ProjectId),
                codeCellId.Id);
    }
}