//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.CodeAnalysis
{
    [Serializable]
    sealed class EvaluationRequest : MainThreadRequest<Evaluation>
    {
        public Compilation Compilation { get; }

        public EvaluationRequest (Compilation compilation)
            => Compilation = compilation
                ?? throw new ArgumentNullException (nameof (compilation));

        protected override bool CanReturnNull => true;

        protected override async Task<Evaluation> HandleAsync (Agent agent)
        {
            agent.ChangeDirectory (Compilation.EvaluationEnvironment.WorkingDirectory);
            await agent
                .GetEvaluationContext (Compilation.EvaluationContextId)
                .RunAsync (MessageId, Compilation);
            return null;
        }
    }
}