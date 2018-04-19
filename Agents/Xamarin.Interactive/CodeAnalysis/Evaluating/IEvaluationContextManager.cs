// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    public interface IEvaluationContextManager
    {
        IObservable<ICodeCellEvent> Events { get; }

        Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (
            CancellationToken cancellationToken = default);

        Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (
            TargetCompilationConfiguration initialConfiguration,
            CancellationToken cancellationToken = default);

        Task ResetStateAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AssemblyLoadResult>> LoadAssembliesAsync (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies,
            CancellationToken cancellationToken = default);

        Task EvaluateAsync (
            EvaluationContextId evaluationContextId,
            Compilation compilation,
            CancellationToken cancellationToken = default);

        Task AbortEvaluationAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken = default);
    }
}