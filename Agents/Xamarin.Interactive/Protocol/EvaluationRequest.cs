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
using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class EvaluationRequest : MainThreadRequest<bool>
    {
        public Guid MessageId { get; }
        public Compilation Compilation { get; }

        [JsonConstructor]
        public EvaluationRequest (
            Guid messageId,
            Compilation compilation)
        {
            MessageId = messageId;
            Compilation = compilation
                ?? throw new ArgumentNullException (nameof (compilation));
        }

        protected override async Task<bool> HandleAsync (Agent agent)
        {
            agent.ChangeDirectory (Compilation.EvaluationEnvironment.WorkingDirectory);
            await agent
                .EvaluationContextManager
                .GetEvaluationContext (Compilation.EvaluationContextId)
                .EvaluateAsync (MessageId, Compilation);
            return true;
        }
    }
}