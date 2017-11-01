//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Representations;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class GetObjectMembersRequest : MainThreadRequest<InteractiveObject>
    {
        protected override bool CanReturnNull => true;

        public long ViewHandle { get; set; }

        protected override Task<InteractiveObject> HandleAsync (Agent agent)
        {
            var members = agent.RepresentationManager.PrepareInteractiveObject (
                ObjectCache.Shared.GetObject (ViewHandle));
            members?.Interact (new InteractiveObject.ReadAllMembersInteractMessage ());
            return Task.FromResult (members);
        }
    }
}