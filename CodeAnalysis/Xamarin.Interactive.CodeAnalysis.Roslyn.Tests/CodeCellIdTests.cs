// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Microsoft.CodeAnalysis;

using Xunit;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    public sealed class CodeCellIdTests
    {
        [Fact]
        public void DocumentIdConversions ()
        {
            var documentId = DocumentId.CreateNewId (ProjectId.CreateNewId ());
            var codeCellId = documentId.ToCodeCellId ();
            var documentIdRoundTrip = codeCellId.ToDocumentId ();
            var codeCellIdRoundTrip = documentIdRoundTrip.ToCodeCellId ();
            Assert.Equal (documentId, documentIdRoundTrip);
            Assert.Equal (codeCellId, codeCellIdRoundTrip);
            Assert.Equal (codeCellId.Id, codeCellIdRoundTrip.Id);
            Assert.Equal (codeCellId.ProjectId, codeCellIdRoundTrip.ProjectId);
            Assert.Equal (documentId.Id, codeCellId.Id);
            Assert.Equal (documentId.ProjectId.Id, codeCellId.ProjectId);
        }

        [Fact]
        public void Empty ()
        {
            CodeCellId emptySubmissionId = default;
            DocumentId emptyDocumentId = default;
            DocumentId convertedSubmissionId = emptySubmissionId.ToDocumentId ();
            Assert.Equal (emptyDocumentId, convertedSubmissionId);
        }
    }
}