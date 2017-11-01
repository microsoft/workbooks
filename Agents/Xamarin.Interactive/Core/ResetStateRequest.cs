//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class ResetStateRequest : MainThreadRequest<SuccessResponse>
    {
        protected override Task<SuccessResponse> HandleAsync (Agent agent)
        {
            agent.ResetState ();
            return SuccessResponse.Task;
        }
    }
}