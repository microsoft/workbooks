//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Evaluating;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Serialization;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Client
{
    sealed class AgentClient : IEvaluationContextManager
    {
        const string TAG = nameof (AgentClient);

        readonly HttpClient httpClient;
        readonly TargetCompilationConfiguration initialTargetCompilationConfiguration;

        /// <summary>
        /// Any request that should await a response delivered through the out-of-band
        /// message channel should register an awaiter and await the request task and
        /// the registered awaiter task (e.g. <see cref="EvaluateAsync"/>).
        /// </summary>
        readonly ConcurrentDictionary<object, TaskCompletionSource> messageChannelAwaiters
            = new ConcurrentDictionary<object, TaskCompletionSource> ();

        readonly Observable<object> messages = new Observable<object> ();
        public IObservable<object> Messages => messages;

        public CancellationToken SessionCancellationToken { get; set; }

        public AgentClient (
            string host,
            ushort port,
            IReadOnlyList<string> assemblySearchPaths)
        {
            initialTargetCompilationConfiguration = TargetCompilationConfiguration
                .CreateInitialForCompilationWorkspace (assemblySearchPaths);

            if (string.IsNullOrEmpty (host))
                host = IPAddress.Loopback.ToString ();

            httpClient = new HttpClient {
                Timeout = Timeout.InfiniteTimeSpan,
                BaseAddress = new Uri ($"http://{host}:{port}/")
            };
        }

        CancellationToken GetCancellationToken (CancellationToken cancellationToken)
            => SessionCancellationToken.LinkWith (cancellationToken);

        /// <summary>
        /// Requests the agent's identity. Should only be called from implementors
        /// or managers of <see cref="IAgentTicket"/>, and as such, does not make
        /// use of the session cancellation token.
        /// </summary>
        public Task<AgentIdentity> GetAgentIdentityAsync (
            CancellationToken cancellationToken = default (CancellationToken))
            => SendAsync<AgentIdentity> (
                new AgentIdentityRequest (),
                cancellationToken);

        /// <summary>
        /// Opens an agent-to-client asynchronous message channel. This method will block/await
        /// for the lifetime of the connection to the agent. Messages sent by the client will
        /// be passed to <see cref="AgentMessageHandler"/>. Does not make use of the session
        /// cancellation token.
        /// </summary>
        public async Task OpenAgentMessageChannel (
            CancellationToken cancellationToken = default (CancellationToken))
        {
            var request = new OpenMessageChannelRequest (
                Guid.NewGuid (),
                Process.GetCurrentProcess ().Id);

            try {
                await Task.Run (() => {
                    SendAsync (request, message => {
                        object awaiterKey = null;

                        switch (message) {
                        case LogEntry logEntry:
                            Log.Commit (logEntry);
                            break;
                        case MessageChannel.Ping ping:
                            break;
                        case ICodeCellEvent evnt:
                            if (evnt is Evaluation)
                                awaiterKey = evnt.CodeCellId;

                            try {
                                codeCellEvents.Observers.OnNext (evnt);
                            } catch (Exception e) {
                                Log.Error (TAG, $"Exception in {nameof (ICodeCellEvent)} observer", e);
                            }
                            break;
                        default:
                            try {
                                messages.Observers.OnNext (message);
                            } catch (Exception e) {
                                Log.Error (TAG, "Exception in message channel observer", e);
                            }
                            break;
                        }

                        if (awaiterKey != null &&
                            messageChannelAwaiters.TryRemove (awaiterKey, out var awaiter))
                            awaiter.SetResult ();
                    }, cancellationToken).GetAwaiter ().GetResult ();
                }, cancellationToken);
            } catch {
            }

            foreach (var entry in messageChannelAwaiters) {
                if (messageChannelAwaiters.TryRemove (entry.Key, out var awaiter)) {
                    try {
                        throw new Exception ("message channel has terminated");
                    } catch (Exception e) {
                        awaiter.SetException (e);
                    }
                }
            }

            messages.Observers.OnCompleted ();
        }

        public Task<AgentFeatures> GetAgentFeaturesAsync (
            CancellationToken cancellationToken = default (CancellationToken))
            => SendAsync<AgentFeatures> (
                new AgentFeaturesRequest (),
                GetCancellationToken (cancellationToken));

        public Task<InspectView> GetVisualTreeAsync (
            string hierarchyKind,
            bool captureViews = true)
            => SendAsync<InspectView> (
                new VisualTreeRequest (hierarchyKind, captureViews),
                SessionCancellationToken);

        public Task<InteractiveObject> GetObjectMembersAsync (long viewHandle)
            => SendAsync<InteractiveObject> (
                new GetObjectMembersRequest (viewHandle),
                SessionCancellationToken);

        public async Task<SetObjectMemberResponse> SetObjectMemberAsync (
            long objHandle,
            RepresentedMemberInfo memberInfo,
            object value,
            bool returnUpdatedValue)
            => (await SendAsync<SetObjectMemberResponse> (
                new SetObjectMemberRequest (
                    objHandle,
                    memberInfo,
                    value,
                    returnUpdatedValue),
                SessionCancellationToken));

        public async Task<T> HighlightView<T> (
            double x,
            double y,
            bool clear,
            string hierarchyKind,
            CancellationToken cancellationToken = default (CancellationToken))
            where T : InspectView
        {
            T result = null;

            // NOTE: Not using simpler SendAsync override because we don't want to throw on error
            await SendAsync (
                new HighlightViewRequest (x, y, clear, hierarchyKind),
                response => result = response as T,
                GetCancellationToken (cancellationToken));

            return result;
        }

        public Task<IInteractiveObject> InteractAsync (IInteractiveObject obj, object message)
            => SendAsync<IInteractiveObject> (
                new InteractRequest (obj.Handle, message),
                SessionCancellationToken);

        public Task SetLogLevelAsync (LogLevel newLogLevel)
            => SendAsync<bool> (new SetLogLevelRequest (newLogLevel));

        async Task SendAsync (
            object message,
            Action<object> responseHandler,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (message == null)
                throw new ArgumentNullException (nameof (message));

            if (responseHandler == null)
                throw new ArgumentNullException (nameof (responseHandler));

            var serializer = InteractiveJsonSerializerSettings
                .SharedInstance
                .CreateSerializer ();

            var response = await httpClient.SendAsync (
                new HttpRequestMessage (HttpMethod.Post, "/api/v1") {
                    Content = new XipRequestMessageHttpContent (serializer, message)
                },
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var bodyContent = await response.Content.ReadAsStringAsync ();
                Log.Error (TAG, $"Server returned error, body content is: {bodyContent}.");
                response.EnsureSuccessStatusCode ();
            }

            using (var contentStream = await response.Content.ReadAsStreamAsync ())
                await serializer.DeserializeMultiple (
                    contentStream,
                    responseHandler,
                    cancellationToken);
        }

        async Task<TResult> SendAsync<TResult> (
            object message,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            TResult result = default(TResult);
            XipErrorMessage errorResult = null;

            await SendAsync (message, response => {
                errorResult = response as XipErrorMessage;
                if (errorResult == null)
                    result = (TResult)response;
            }, cancellationToken);

            if (errorResult != null) {
                Log.Error (TAG, $"SendAsync ({message.GetType ()}) -> " +
                           $"{typeof (TResult)}: {errorResult}");
                errorResult.Throw ();
            }

            return result;
        }

        #region IEvaluationContextManager

        readonly Observable<ICodeCellEvent> codeCellEvents = new Observable<ICodeCellEvent> ();
        IObservable<ICodeCellEvent> IEvaluationContextManager.Events => codeCellEvents;

        Task<TargetCompilationConfiguration> IEvaluationContextManager.CreateEvaluationContextAsync (
            CancellationToken cancellationToken)
            => SendAsync<TargetCompilationConfiguration> (
                new EvaluationContextInitializeRequest (initialTargetCompilationConfiguration),
                GetCancellationToken (cancellationToken));

        Task<TargetCompilationConfiguration> IEvaluationContextManager.CreateEvaluationContextAsync (
            TargetCompilationConfiguration initialConfiguration,
            CancellationToken cancellationToken)
            => SendAsync<TargetCompilationConfiguration> (
                new EvaluationContextInitializeRequest (initialConfiguration),
                GetCancellationToken (cancellationToken));

        Task IEvaluationContextManager.ResetStateAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken)
            => SendAsync<bool> (
                new ResetStateRequest (evaluationContextId),
                GetCancellationToken (cancellationToken));

        async Task<IReadOnlyList<AssemblyLoadResult>> IEvaluationContextManager.LoadAssembliesAsync (
            EvaluationContextId evaluationContextId,
            IReadOnlyList<AssemblyDefinition> assemblies,
            CancellationToken cancellationToken)
        {
            var response = await SendAsync<AssemblyLoadResponse> (
                new AssemblyLoadRequest (evaluationContextId, assemblies),
                GetCancellationToken (cancellationToken));

            return response.LoadResults;
        }

        Task IEvaluationContextManager.AbortEvaluationAsync (
            EvaluationContextId evaluationContextId,
            CancellationToken cancellationToken)
            => SendAsync<bool> (
                new EvaluationAbortRequest (evaluationContextId),
                GetCancellationToken (cancellationToken));

        async Task IEvaluationContextManager.EvaluateAsync (
            EvaluationContextId evaluationContextId,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var responseTask = new TaskCompletionSource ();

            if (!messageChannelAwaiters.TryAdd (compilation.CodeCellId, responseTask))
                throw new InvalidOperationException (
                    "An evaluation for cell {compilation.CodeCellId} is already in process");

            await Task.WhenAll (
                responseTask.Task,
                SendAsync<bool> (
                     new EvaluationRequest (
                        evaluationContextId,
                        compilation),
                    GetCancellationToken (cancellationToken)));
        }

        #endregion

        sealed class XipRequestMessageHttpContent : HttpContent
        {
            readonly JsonSerializer serializer;
            readonly object message;

            public XipRequestMessageHttpContent (JsonSerializer serializer, object message)
            {
                this.serializer = serializer;
                this.message = message;

                EnsureHeaders ();
            }

            void EnsureHeaders ()
            {
                // Mono's HttpContent seems to re-allocate 'Headers' some time after
                // the constructor finishes, clobbering anything configured during
                // the constructor. However, it seems to write them on the first call
                // to Stream.Write for the Stream passed to SerializeToStreamAsync,
                // so the fix on Mono is to configure Headers before writing to the
                // stream. This however is not the behavior of .NET, which requires
                // Headers to be configured in the constructor (it likely writes them
                // explicitly to the stream before calling SerializeToStreamAsync,
                // and not as an implementation of the Stream itself. So do both.
                Headers.ContentType = new MediaTypeHeaderValue ("application/octet-stream");
            }

            protected override Task SerializeToStreamAsync (Stream stream, TransportContext context)
            {
                EnsureHeaders ();

                serializer.Serialize (stream, message);

                return stream.FlushAsync ();
            }

            protected override bool TryComputeLength (out long length)
            {
                length = -1;
                return false;
            }
        }
    }
}