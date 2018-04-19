// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis.Resolving;

namespace Xamarin.Interactive.CodeAnalysis
{
    [JsonObject]
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
                default,
                new List<string> (assemblySearchPaths ?? Array.Empty<string> ()),
                default);

        internal HostOS CompilationOS { get; }
        public EvaluationContextId EvaluationContextId { get; }
        public TypeDefinition GlobalStateType { get; }
        public IReadOnlyList<string> DefaultImports { get; }
        public IReadOnlyList<string> DefaultWarningSuppressions { get; }
        public IReadOnlyList<AssemblyDefinition> InitialReferences { get; }
        public IReadOnlyList<string> AssemblySearchPaths { get; }
        public bool IncludePEImagesInDependencyResolution { get; }

        [JsonConstructor]
        TargetCompilationConfiguration (
            HostOS compilationOS,
            EvaluationContextId evaluationContextId,
            TypeDefinition globalStateType,
            IReadOnlyList<string> defaultImports,
            IReadOnlyList<string> defaultWarningSuppressions,
            IReadOnlyList<AssemblyDefinition> initialReferences,
            IReadOnlyList<string> assemblySearchPaths,
            bool includePEImagesInDependencyResolution)
        {
            CompilationOS = compilationOS;
            EvaluationContextId = evaluationContextId;
            GlobalStateType = globalStateType;
            DefaultImports = defaultImports ?? Array.Empty<string> ();
            DefaultWarningSuppressions = defaultWarningSuppressions ?? Array.Empty<string> ();
            InitialReferences = initialReferences ?? Array.Empty<AssemblyDefinition> ();
            AssemblySearchPaths = assemblySearchPaths ?? Array.Empty<string> ();
            IncludePEImagesInDependencyResolution = includePEImagesInDependencyResolution;
        }

        internal TargetCompilationConfiguration With (
            Optional<HostOS> compilationOS = default,
            Optional<EvaluationContextId> evaluationContextId = default,
            Optional<TypeDefinition> globalStateType = default,
            Optional<IReadOnlyList<string>> defaultImports = default,
            Optional<IReadOnlyList<string>> defaultWarningSuppressions = default,
            Optional<IReadOnlyList<AssemblyDefinition>> initialReferences = default,
            Optional<IReadOnlyList<string>> assemblySearchPaths = default,
            Optional<bool> includePEImagesInDependencyResolution = default)
            => new TargetCompilationConfiguration (
                compilationOS.GetValueOrDefault (CompilationOS),
                evaluationContextId.GetValueOrDefault (EvaluationContextId),
                globalStateType.GetValueOrDefault (GlobalStateType),
                defaultImports.GetValueOrDefault (DefaultImports),
                defaultWarningSuppressions.GetValueOrDefault (DefaultWarningSuppressions),
                initialReferences.GetValueOrDefault (InitialReferences),
                assemblySearchPaths.GetValueOrDefault (AssemblySearchPaths),
                includePEImagesInDependencyResolution.GetValueOrDefault (IncludePEImagesInDependencyResolution));
    }
}