// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class SetLogLevelRequest : MainThreadRequest<bool>
    {
        public LogLevel LogLevel { get; }

        [JsonConstructor]
        public SetLogLevelRequest (LogLevel logLevel)
            => LogLevel = logLevel;

        protected override Task<bool> HandleAsync (Agent agent)
        {
            agent.SetLogLevel (LogLevel);
            return Task.FromResult (true);
        }
    }
}