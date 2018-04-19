// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class ResetStateRequest : MainThreadRequest<bool>
    {
        protected override Task<bool> HandleAsync (Agent agent)
        {
            agent.EvaluationContextManager.ResetState ();
            return Task.FromResult (true);
        }
    }
}