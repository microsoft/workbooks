// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

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
    }
}