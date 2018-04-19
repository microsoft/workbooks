// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class GetObjectMembersRequest : MainThreadRequest<InteractiveObject>
    {
        public long ViewHandle { get; }

        [JsonConstructor]
        public GetObjectMembersRequest (long viewHandle)
            => ViewHandle = viewHandle;

        protected override Task<InteractiveObject> HandleAsync (Agent agent)
        {
            var members = agent
                .RepresentationManager
                .PrepareInteractiveObject (
                    ObjectCache.Shared.GetObject (ViewHandle));
            members?.Interact (new InteractiveObject.ReadAllMembersInteractMessage ());
            return Task.FromResult (members);
        }
    }
}