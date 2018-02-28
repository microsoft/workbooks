//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Xamarin.Interactive.Compilation;
using Xamarin.Interactive.Compilation.Roslyn;

namespace Xamarin.Interactive.CodeAnalysis
{
    public sealed class EvaluationServiceTests
    {
        [Fact]
        internal RoslynCompilationWorkspace CreateWorkspace ()
        {
            var agentType = AgentType.DotNetCore;

            var dependencyResolver = new InteractiveDependencyResolver (agentType);
            dependencyResolver.AddDefaultReferences (Array.Empty<AssemblyDefinition> ());

            return new RoslynCompilationWorkspace (
                dependencyResolver,
                new TargetCompilationConfiguration {
                    DefaultUsings = new [] { "System" },
                    DefaultWarningSuppressions = Array.Empty<string> ()
                },
                agentType);
        }

        [Fact]
        internal EvaluationService CreateEvaluationService ()
            => new EvaluationService (
                CreateWorkspace (),
                new EvaluationEnvironment (Environment.CurrentDirectory),
                null);

        [Fact]
        public async Task Append ()
        {
            var controller = CreateEvaluationService ();

            var submissionA = await controller.InsertCodeCellAsync ("buffer-a", default);
            var submissionB = await controller.InsertCodeCellAsync ("buffer-b", submissionA.Id);
            var submissionC = await controller.InsertCodeCellAsync ("buffer-c", submissionB.Id);
            var submissionD = await controller.InsertCodeCellAsync ("buffer-d", submissionC.Id);

            var all = (await controller.GetAllCodeCellsAsync ()).ToList ();

            Assert.Equal (submissionA.Id, all [0].Id);
            Assert.Equal ("buffer-a", all [0].Buffer.Value);

            Assert.Equal (submissionB.Id, all [1].Id);
            Assert.Equal ("buffer-b", all [1].Buffer.Value);

            Assert.Equal (submissionC.Id, all [2].Id);
            Assert.Equal ("buffer-c", all [2].Buffer.Value);

            Assert.Equal (submissionD.Id, all [3].Id);
            Assert.Equal ("buffer-d", all [3].Buffer.Value);
        }

        [Fact]
        public async Task Insert ()
        {
            var service = CreateEvaluationService ();

            var submissionC = await service.InsertCodeCellAsync ("buffer-c", default);
            var submissionD = await service.InsertCodeCellAsync ("buffer-d", submissionC.Id);
            var submissionB = await service.InsertCodeCellAsync ("buffer-b", submissionC.Id, true);
            var submissionA = await service.InsertCodeCellAsync ("buffer-a", submissionB.Id, true);

            var all = (await service.GetAllCodeCellsAsync ()).ToList ();

            Assert.Equal (submissionA.Id, all [0].Id);
            Assert.Equal ("buffer-a", all [0].Buffer.Value);

            Assert.Equal (submissionB.Id, all [1].Id);
            Assert.Equal ("buffer-b", all [1].Buffer.Value);

            Assert.Equal (submissionC.Id, all [2].Id);
            Assert.Equal ("buffer-c", all [2].Buffer.Value);

            Assert.Equal (submissionD.Id, all [3].Id);
            Assert.Equal ("buffer-d", all [3].Buffer.Value);
        }

        [Fact]
        public async Task Model ()
        {
            var service = CreateEvaluationService ();

            await service.InsertCodeCellAsync ("var a = 10");
            await service.InsertCodeCellAsync ("var b = a * 2");

            var allSubmissions = await service.GetAllCodeCellsAsync ();
            var model = await service.GetEvaluationModelAsync ();

            Assert.True (model.ShouldResetAgentState);
            Assert.Collection (
                model.CellsToEvaluate,
                s => Assert.Equal (allSubmissions [0].Id, s.Id),
                s => Assert.Equal (allSubmissions [1].Id, s.Id));
        }
    }
}