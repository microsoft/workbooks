// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Protocol
{
    abstract class MainThreadRequest<TResponseMessage> : IXipRequestMessage<Agent>
    {
        protected abstract Task<TResponseMessage> HandleAsync (Agent agent);

        public void Handle (Agent agent, Action<object> responseWriter)
        {
            object result = null;

            Task.Factory.StartNew (
                async () => {
                    try {
                        result = await HandleAsync (agent);
                    } catch (Exception e) {
                        result = new XipErrorMessage (
                            $"{GetType ()}.Handle(Agent) threw an exception",
                            e);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                MainThread.TaskScheduler).Unwrap ().Wait ();

            responseWriter (result);
        }
    }
}