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
    sealed class InteractRequest : MainThreadRequest<IInteractiveObject>
    {
        public long InteractiveObjectHandle { get; }
        public object Message { get; }

        [JsonConstructor]
        public InteractRequest (long interactiveObjectHandle, object message)
        {
            InteractiveObjectHandle = interactiveObjectHandle;
            Message = message;
        }

        protected override Task<IInteractiveObject> HandleAsync (Agent agent)
        {
            var result = ObjectCache.Shared.GetObject (InteractiveObjectHandle) as IInteractiveObject;
            if (result != null)
                result = result.Interact (Message);
            return Task.FromResult (result);
        }
    }
}