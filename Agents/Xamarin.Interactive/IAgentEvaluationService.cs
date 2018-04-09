// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    public interface IAgentEvaluationService
    {
        IObservable<ICodeCellEvent> Events { get; }

        EvaluationContextId EvaluationContextId { get; }

        Task<TargetCompilationConfiguration> InitializeAsync (
            bool includePeImage,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AssemblyDefinition>> GetAppDomainAssembliesAsync (
            bool includePeImage,
            CancellationToken cancellationToken = default);

        Task ResetStateAsync (CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AssemblyLoadResult>> LoadAssembliesAsync (
            IReadOnlyList<AssemblyDefinition> assemblies,
            CancellationToken cancellationToken = default);

        Task EvaluateAsync (
            Compilation compilation,
            CancellationToken cancellationToken = default);
    }
}