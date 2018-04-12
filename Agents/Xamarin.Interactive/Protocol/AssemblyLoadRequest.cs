//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class AssemblyLoadRequest : MainThreadRequest<AssemblyLoadResponse>
    {
        const string TAG = nameof (AssemblyLoadRequest);

        public EvaluationContextId EvaluationContextId { get; }
        public AssemblyDefinition [] Assemblies { get; }

        [JsonConstructor]
        public AssemblyLoadRequest (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies)
        {
            EvaluationContextId = evaluationContextId;
            Assemblies = assemblies?.ToArray ();
        }

        protected override Task<AssemblyLoadResponse> HandleAsync (Agent agent)
        {
            var scriptContext = agent.GetEvaluationContext (EvaluationContextId);

            var results = new AssemblyLoadResult [Assemblies.Length];

            scriptContext.AssemblyContext.AddRange (Assemblies);
            for (var i = 0; i < Assemblies.Length; i++) {
                AssemblyDefinition asm = Assemblies [i];
                bool didLoad = false;
                bool initializedAgentIntegration = false;

                try {
                    Assembly loadedAsm = null;

                    if (asm.Content.Location.FileExists) {
                        loadedAsm = Assembly.LoadFrom (asm.Content.Location);
                        didLoad = true;
                    } else if (asm.Content.PEImage != null) {
                        loadedAsm = Assembly.Load (
                            asm.Content.PEImage,
                            asm.Content.DebugSymbols);
                        didLoad = true;
                    } else {
                        Log.Warning (
                            TAG,
                            $"Could not load assembly name {asm.Name}, location didn't " +
                            "exist and PE image wasn't sent.");
                    }

                    if (loadedAsm != null)
                        initializedAgentIntegration = scriptContext
                            .CheckLoadedAssemblyForAgentIntegration (loadedAsm) != null;
                } catch (Exception e) {
                    Log.Error (TAG, $"Could not load sent assembly {asm.Name}: {e.Message}", e);
                } finally {
                    results [i] = new AssemblyLoadResult (
                        asm.Name,
                        didLoad,
                        initializedAgentIntegration);
                }
            }

            return Task.FromResult (new AssemblyLoadResponse (results));
        }
    }
}