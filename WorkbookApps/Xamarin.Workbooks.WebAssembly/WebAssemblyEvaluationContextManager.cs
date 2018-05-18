//
// Authors:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive;
using Xamarin.Interactive.CodeAnalysis.Evaluating;

namespace Xamarin.Workbooks.WebAssembly
{
    sealed class WebAssemblyEvaluationContextManager : EvaluationContextManager
    {
        internal WebAssemblyEvaluationContextManager (WebAssemblyAgent agent)
            : base (agent.RepresentationManager, agent)
        {
        }

        public override IAgentSynchronizationContext SynchronizationContexts => null;

        protected override object CreateGlobalState ()
            => new EvaluationContextGlobalObject ((WebAssemblyAgent)Context);
    }
}