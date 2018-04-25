// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Representations;

using Xunit;

namespace Xamarin.Interactive.CodeAnalysis.Roslyn
{
    public sealed class EvaluationTests
    {
        [Fact]
        public async Task TwoPlusTwo ()
        {
            var evaluationContextManager = new EvaluationContextManager (
                new RepresentationManager (RepresentationManagerOptions.YieldOriginal)
            );

            var workspaceConfiguration = await WorkspaceConfiguration
                .CreateAsync (evaluationContextManager);

            var workspaceService = await WorkspaceServiceFactory
                .CreateWorkspaceServiceAsync ("csharp", workspaceConfiguration);

            var evaluationService = new EvaluationService (
                workspaceService,
                default,
                evaluationContextManager);

            var events = new CodeCellEventList (e => e is Evaluation);

            evaluationService.Events.Subscribe (events);

            var firstCell = await evaluationService.InsertCodeCellAsync ("var x = 20");
            var secondCell = await evaluationService.InsertCodeCellAsync ("x *= x");
            var thirdCell = await evaluationService.InsertCodeCellAsync ("x * x * x");
            var fourthCell = await evaluationService.InsertCodeCellAsync ("x");

            await evaluationService.EvaluateAsync ();

            void AssertResult (ICodeCellEvent evnt, CodeCellId cellId, object expected)
            {
                var evaluation = Assert.IsType<Evaluation> (evnt);
                Assert.Equal (cellId, evaluation.CodeCellId);
                Assert.Equal (expected, evaluation.ResultRepresentations [0]);
            }

            Assert.Collection (
                events,
                e => AssertResult (e, firstCell, 20),
                e => AssertResult (e, secondCell, 400),
                e => AssertResult (e, thirdCell, 64000000),
                e => AssertResult (e, fourthCell, 400));
        }

        sealed class CodeCellEventList : IObserver<ICodeCellEvent>, IReadOnlyList<ICodeCellEvent>
        {
            readonly List<ICodeCellEvent> list = new List<ICodeCellEvent> ();
            readonly Predicate<ICodeCellEvent> filter;

            public CodeCellEventList (Predicate<ICodeCellEvent> filter = null)
                => this.filter = filter ?? (e => true);

            public int Count => list.Count;
            public ICodeCellEvent this [int index] => list [index];
            public IEnumerator<ICodeCellEvent> GetEnumerator () => list.GetEnumerator ();
            IEnumerator IEnumerable.GetEnumerator () => list.GetEnumerator ();

            void IObserver<ICodeCellEvent>.OnNext (ICodeCellEvent value)
            {
                if (filter (value))
                    list.Add (value);
            }

            void IObserver<ICodeCellEvent>.OnCompleted ()
            {
            }

            void IObserver<ICodeCellEvent>.OnError (Exception error)
            {
            }
        }
    }
}