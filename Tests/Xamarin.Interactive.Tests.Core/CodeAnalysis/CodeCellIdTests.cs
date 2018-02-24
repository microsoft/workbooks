//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Microsoft.CodeAnalysis;

using Xunit;

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
            var submissionId = documentId.ToCodeCellId ();
            var documentIdRoundTrip = submissionId.ToDocumentId ();
            var submissionIdRoundTrip = documentIdRoundTrip.ToCodeCellId ();
            Assert.Equal (documentId, documentIdRoundTrip);
            Assert.Equal (submissionId, submissionIdRoundTrip);
            Assert.Equal (submissionId.Id, submissionIdRoundTrip.Id);
            Assert.Equal (submissionId.ProjectId, submissionIdRoundTrip.ProjectId);
            Assert.Equal (documentId.Id, submissionId.Id);
            Assert.Equal (documentId.ProjectId.Id, submissionId.ProjectId);
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