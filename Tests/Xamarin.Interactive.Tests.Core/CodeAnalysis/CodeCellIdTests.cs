// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Microsoft.CodeAnalysis;

using Xunit;

using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis
{
    public sealed class SubmissionIdTests
    {
        [Fact]
        public void Parse ()
        {
            var projectId = Guid.NewGuid ();
            var id = Guid.NewGuid ();
            var submissionId = CodeCellId.Parse ($"{projectId}/{id}");
            Assert.Equal (projectId, submissionId.ProjectId);
            Assert.Equal (id, submissionId.Id);
        }

        [Fact]
        public void Create ()
        {
            var projectId = Guid.NewGuid ();
            var id = Guid.NewGuid ();
            var submissionId = new CodeCellId (projectId, id);
            Assert.Equal (projectId, submissionId.ProjectId);
            Assert.Equal (id, submissionId.Id);
        }

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