// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    public sealed class TargetCompilationConfiguration
    {
        public static TargetCompilationConfiguration CreateInitialForCompilationWorkspace (
            IReadOnlyList<string> assemblySearchPaths = null)
            => new TargetCompilationConfiguration (
                HostEnvironment.OS,
                default,
                default,
                default,
                default,
                new List<string> (assemblySearchPaths ?? Array.Empty<string> ()),
                default);

        internal HostOS CompilationOS { get; }
        public EvaluationContextId EvaluationContextId { get; }
        public TypeDefinition GlobalStateType { get; }
        public IReadOnlyList<string> DefaultImports { get; }
        public IReadOnlyList<string> DefaultWarningSuppressions { get; }
        public IReadOnlyList<string> AssemblySearchPaths { get; }
        public bool IncludePEImagesInDependencyResolution { get; }

        TargetCompilationConfiguration (
            HostOS compilationOS,
            EvaluationContextId evaluationContextId,
            TypeDefinition globalStateType,
            IReadOnlyList<string> defaultImports,
            IReadOnlyList<string> defaultWarningSuppressions,
            IReadOnlyList<string> assemblySearchPaths,
            bool includePEImagesInDependencyResolution)
        {
            CompilationOS = compilationOS;
            EvaluationContextId = evaluationContextId;
            GlobalStateType = globalStateType;
            DefaultImports = defaultImports;
            DefaultWarningSuppressions = defaultWarningSuppressions;
            AssemblySearchPaths = assemblySearchPaths;
            IncludePEImagesInDependencyResolution = includePEImagesInDependencyResolution;
        }

        internal TargetCompilationConfiguration With (
            Optional<HostOS> compilationOS = default,
            Optional<EvaluationContextId> evaluationContextId = default,
            Optional<TypeDefinition> globalStateType = default,
            Optional<IReadOnlyList<string>> defaultImports = default,
            Optional<IReadOnlyList<string>> defaultWarningSuppressions = default,
            Optional<IReadOnlyList<string>> assemblySearchPaths = default,
            Optional<bool> includePEImagesInDependencyResolution = default)
            => new TargetCompilationConfiguration (
                compilationOS.GetValueOrDefault (CompilationOS),
                evaluationContextId.GetValueOrDefault (EvaluationContextId),
                globalStateType.GetValueOrDefault (GlobalStateType),
                defaultImports.GetValueOrDefault (DefaultImports),
                defaultWarningSuppressions.GetValueOrDefault (DefaultWarningSuppressions),
                assemblySearchPaths.GetValueOrDefault (AssemblySearchPaths),
                includePEImagesInDependencyResolution.GetValueOrDefault (IncludePEImagesInDependencyResolution));
    }
}