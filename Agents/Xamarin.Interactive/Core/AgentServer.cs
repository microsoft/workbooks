//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Protocol
{
    sealed class AgentServer : HttpServer
    {
        const string TAG = nameof (AgentServer);

        readonly Agent agent;

        public new Uri BaseUri => base.BaseUri;

        public AgentServer (Agent agent)
            => this.agent = agent ?? throw new ArgumentNullException (nameof (agent));

        public new void Start ()
            => base.Start ();

        public new void Stop ()
            => base.Stop ();

        protected override Task PerformHttpAsync (HttpListenerContext context, CancellationToken cancellationToken)
        {
            try {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                var serializer = InteractiveJsonSerializerSettings
                    .SharedInstance
                    .CreateSerializer ();

                var requestObject = serializer.Deserialize (context.Request.InputStream);
                if (requestObject == null) {
                    Log.Warning (TAG, "Accept: value must not be null");
                    return Task.CompletedTask;
                }

                var message = requestObject as IXipRequestMessage<Agent>;
                if (message == null) {
                    Log.Warning (TAG, "Accept: expected IXipRequestMessage " +
                        $"(got a {requestObject.GetType ()})");
                    return Task.CompletedTask;
                }

                Log.Debug (TAG, $"Accept: received message: {message.GetType ()}");

                message.Handle (agent, result => {
                    serializer.Serialize (context.Response.OutputStream, result);
                    context.Response.OutputStream.Flush ();
                });
            } catch (Exception e) {
                Log.Error (TAG, e);
            }

            return Task.CompletedTask;
        }
    }
}