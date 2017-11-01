//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class SetLogLevelRequest : MainThreadRequest<SuccessResponse>
    {
        public LogLevel LogLevel { get; set; }

        protected override Task<SuccessResponse> HandleAsync (Agent agent)
        {
            agent.SetLogLevel (LogLevel);
            return SuccessResponse.Task;
        }
    }
}
