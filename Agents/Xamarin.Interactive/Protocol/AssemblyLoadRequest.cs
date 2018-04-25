// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class AssemblyLoadRequest : MainThreadRequest<AssemblyLoadResponse>
    {
        public EvaluationContextId EvaluationContextId { get; }
        public IReadOnlyList<AssemblyDefinition> Assemblies { get; }

        [JsonConstructor]
        public AssemblyLoadRequest (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies)
        {
            EvaluationContextId = evaluationContextId;
            Assemblies = assemblies ?? Array.Empty<AssemblyDefinition> ();
        }

        protected override async Task<AssemblyLoadResponse> HandleAsync (Agent agent)
            => new AssemblyLoadResponse (
                await agent.EvaluationContextManager.LoadAssembliesAsync (
                    EvaluationContextId,
                    Assemblies));
    }
}