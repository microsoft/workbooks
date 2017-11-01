//
// InteractRequest.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

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