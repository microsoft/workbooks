//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class EvaluationRequest : MainThreadRequest<bool>
    {
        public EvaluationContextId EvaluationContextId { get; }
        public Guid MessageId { get; }
        public Compilation Compilation { get; }

        [JsonConstructor]
        public EvaluationRequest (
            EvaluationContextId evaluationContextId,
            Guid messageId,
            Compilation compilation)
        {
            EvaluationContextId = evaluationContextId;
            MessageId = messageId;
            Compilation = compilation
                ?? throw new ArgumentNullException (nameof (compilation));
        }

        protected override async Task<bool> HandleAsync (Agent agent)
        {
            agent.ChangeDirectory (Compilation.EvaluationEnvironment.WorkingDirectory);
            await agent
                .EvaluationContextManager
                .GetEvaluationContext (EvaluationContextId)
                .EvaluateAsync (MessageId, Compilation);
            return true;
        }
    }
}