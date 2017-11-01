//
// ResetStateRequest.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

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