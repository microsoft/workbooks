//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Core
{
    sealed class AgentIdentificationManager
    {
        public static TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes (5);

        struct IdentifyAgentRequestHandler
        {
            public TaskCompletionSource<AgentIdentity> TaskCompletionSource;
            public CancellationToken CancellationToken;
            public IdentifyAgentRequest Request;
        }

        readonly Dictionary<Guid, IdentifyAgentRequestHandler> identityHandlers
            = new Dictionary<Guid, IdentifyAgentRequestHandler> ();

        readonly Uri baseUri;

        public AgentIdentificationManager (Uri baseUri)
            => this.baseUri = baseUri ?? throw new ArgumentNullException (nameof (baseUri));

        public bool RespondToAgentIdentityRequest (
            Guid token,
            AgentIdentity identity)
        {
            IdentifyAgentRequestHandler requestHandler;

            lock (identityHandlers) {
                if (!identityHandlers.TryGetValue (token, out requestHandler))
                    return false;

                identityHandlers.Remove (token);
            }

            if (requestHandler.CancellationToken.IsCancellationRequested) {
                requestHandler.TaskCompletionSource.TrySetCanceled ();
                return false;
            }

            requestHandler.TaskCompletionSource.SetResult (identity);

            return true;
        }

        public IdentifyAgentRequest GetAgentIdentityRequest (
            CancellationToken cancellationToken = default (CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested ();

            var request = IdentifyAgentRequest.CreateWithBaseConnectUri (baseUri);

            lock (identityHandlers)
                identityHandlers.Add (request.RequestToken, new IdentifyAgentRequestHandler {
                    TaskCompletionSource = new TaskCompletionSource<AgentIdentity> (),
                    CancellationToken = cancellationToken,
                    Request = request
                });

            return request;
        }

        public async Task<AgentIdentity> GetAgentIdentityAsync (
            IdentifyAgentRequest request,
            TimeSpan timeout = default (TimeSpan))
        {
            IdentifyAgentRequestHandler requestHandler;

            lock (identityHandlers)
                if (!identityHandlers.TryGetValue (request.RequestToken, out requestHandler))
                    throw new KeyNotFoundException (
                        "request has received a response or was not " +
                        "registeredthrough GetAgentIdentityRequest");

            if (timeout == default (TimeSpan))
                timeout = DefaultTimeout;

            var completedTask = await Task.WhenAny (
                requestHandler.TaskCompletionSource.Task,
                Task.Delay (timeout)).ConfigureAwait (false);

            if (completedTask == requestHandler.TaskCompletionSource.Task)
                return requestHandler.TaskCompletionSource.Task.Result;

            throw new TimeoutException (
                $"{request} has timed out ({timeout.TotalSeconds}s)");
        }
    }
}