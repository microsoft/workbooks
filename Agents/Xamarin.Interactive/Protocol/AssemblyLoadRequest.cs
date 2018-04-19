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
using Xamarin.Interactive.CodeAnalysis.Evaluating;
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
        public IReadOnlyList<AssemblyDefinition> Assemblies { get; }

        [JsonConstructor]
        public AssemblyLoadRequest (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies)
        {
            EvaluationContextId = evaluationContextId;
            Assemblies = assemblies ?? Array.Empty<AssemblyDefinition> ();
        }

        protected override Task<AssemblyLoadResponse> HandleAsync (Agent agent)
        {
            var evaluationContext = agent.EvaluationContextManager.GetEvaluationContext (EvaluationContextId);

            var results = new AssemblyLoadResult [Assemblies.Count];

            evaluationContext.AssemblyContext.AddRange (Assemblies);
            for (var i = 0; i < Assemblies.Count; i++) {
                AssemblyDefinition asm = Assemblies [i];
                bool didLoad = false;
                bool initializedIntegration = false;

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
                        initializedIntegration = evaluationContext
                            .Host
                            .TryLoadIntegration (loadedAsm);
                } catch (Exception e) {
                    Log.Error (TAG, $"Could not load sent assembly {asm.Name}: {e.Message}", e);
                } finally {
                    results [i] = new AssemblyLoadResult (
                        asm.Name,
                        didLoad,
                        initializedIntegration);
                }
            }

            return Task.FromResult (new AssemblyLoadResponse (results));
        }
    }
}