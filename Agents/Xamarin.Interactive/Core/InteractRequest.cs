//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class InteractRequest : MainThreadRequest<InteractResponse>
    {
        public long InteractiveObjectHandle { get; set; }
        public object Message { get; set; }

        protected override Task<InteractResponse> HandleAsync (Agent agent)
        {
            var result = ObjectCache.Shared.GetObject (InteractiveObjectHandle) as IInteractiveObject;
            if (result != null)
                result = result.Interact (Message);
            return Task.FromResult (new InteractResponse { Result = result });
        }
    }
}