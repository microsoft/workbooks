//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Protocol;
using Xamarin.Interactive.Serialization;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Client
{
    sealed class AgentClient
    {
        const string TAG = nameof (AgentClient);

        readonly HttpClient httpClient;

        readonly Observable<object> messages = new Observable<object> ();
        public IObservable<object> Messages => messages;

        public CancellationToken SessionCancellationToken { get; set; }

        public AgentClient (string host, ushort port)
        {
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
            var request = new OpenMessageChannelRequest (Process.GetCurrentProcess ().Id);
            var response = new MessageChannelClosedResponse (request.MessageId);

            try {
                await Task.Run (() => {
                    SendAsync (request, message => {
                        switch (message) {
                        case LogEntry logEntry:
                            Log.Commit (logEntry);
                            break;
                        case MessageChannel.Ping ping:
                            break;
                        default:
                            messages.Observers.OnNext (message);
                            break;
                        }
                    }, cancellationToken).GetAwaiter ().GetResult ();
                }, cancellationToken);
            } catch {
            }

            messages.Observers.OnCompleted ();
        }

        public Task<TargetCompilationConfiguration> InitializeEvaluationContextAsync (bool includePeImage)
            => SendAsync<TargetCompilationConfiguration> (
                new EvaluationContextInitializeRequest (includePeImage),
                SessionCancellationToken);

        public Task ResetStateAsync ()
            => SendAsync<SuccessResponse> (
                new ResetStateRequest (),
                SessionCancellationToken);

        public Task AssociateClientSession (
            ClientSessionAssociationKind kind,
            FilePath workingDirectory = default (FilePath))
            => SendAsync<SuccessResponse> (
                new ClientSessionAssociation (kind, workingDirectory),
                SessionCancellationToken);

        public Task<AssemblyDefinition []> GetAppDomainAssembliesAsync (bool includePeImage)
            => SendAsync<AssemblyDefinition []> (
                new GetAppDomainAssembliesRequest (includePeImage),
                SessionCancellationToken);

        public Task<AssemblyLoadResponse> LoadAssembliesAsync (
            EvaluationContextId evaluationContextId,
            AssemblyDefinition [] assemblies)
            => SendAsync<AssemblyLoadResponse> (
                new AssemblyLoadRequest (evaluationContextId, assemblies),
                SessionCancellationToken);

        public Task EvaluateAsync (
            Compilation compilation,
            CancellationToken cancellationToken = default (CancellationToken))
            => SendAsync<Evaluation> (
                new EvaluationRequest (compilation),
                GetCancellationToken (cancellationToken));

        public Task AbortEvaluationAsync (EvaluationContextId evaluationContextId)
            => SendAsync<bool> (
                new AbortEvaluationRequest (evaluationContextId),
                SessionCancellationToken);

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
                new GetObjectMembersRequest { ViewHandle = viewHandle },
                SessionCancellationToken);

        public async Task<SetObjectMemberResponse> SetObjectMemberAsync (
            long objHandle,
            RepresentedMemberInfo memberInfo,
            object value,
            bool returnUpdatedValue)
            => (await SendAsync<SetObjectMemberResponse> (
                new SetObjectMemberRequest {
                    ObjectHandle = objHandle,
                    MemberInfo = memberInfo,
                    Value = value,
                    ReturnUpdatedValue = returnUpdatedValue
                },
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
                new HighlightViewRequest {
                    X = x,
                    Y = y,
                    Clear = clear,
                    HierarchyKind = hierarchyKind,
                },
                response => result = response as T,
                GetCancellationToken (cancellationToken));

            return result;
        }

        public async Task<IInteractiveObject> InteractAsync (IInteractiveObject obj, object message)
            => (await SendAsync<InteractResponse> (
                new InteractRequest {
                    InteractiveObjectHandle = obj.Handle,
                    Message = message
                },
                SessionCancellationToken))?.Result;

        public async Task SetLogLevelAsync (LogLevel newLogLevel)
            => await SendAsync<SuccessResponse> (
                new SetLogLevelRequest {
                    LogLevel = newLogLevel
                });

        async Task SendAsync (
            IXipRequestMessage message,
            Action<object> responseHandler,
            CancellationToken cancellationToken = default (CancellationToken))
        {
            if (message == null)
                throw new ArgumentNullException (nameof (message));

            if (responseHandler == null)
                throw new ArgumentNullException (nameof (responseHandler));

            var response = await httpClient.SendAsync (
                new HttpRequestMessage (HttpMethod.Post, "/api/v1") {
                    Content = new XipRequestMessageHttpContent (message)
                },
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var bodyContent = await response.Content.ReadAsStringAsync ();
                Log.Error (TAG, $"Server returned error, body content is: {bodyContent}.");
                response.EnsureSuccessStatusCode ();
            }

            using (var contentStream = new HttpResponseStream (await response.Content.ReadAsStreamAsync ())) {
                var responseSerializer = new XipSerializer (contentStream, InteractiveSerializerSettings.SharedInstance);

                while (!cancellationToken.IsCancellationRequested) {
                    try {
                        var responseMessage = responseSerializer.Deserialize ();
                        if (responseMessage is XipEndOfMessagesMessage)
                            break;
                        responseHandler (responseMessage);
                    } catch (EndOfStreamException) {
                        break;
                    }
                }
            }
        }

        async Task<TResult> SendAsync<TResult> (
            IXipRequestMessage message,
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

        sealed class XipRequestMessageHttpContent : HttpContent
        {
            readonly IXipRequestMessage message;

            public XipRequestMessageHttpContent (IXipRequestMessage message)
            {
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

                new XipSerializer (stream, InteractiveSerializerSettings.SharedInstance)
                    .Serialize (message);

                return stream.FlushAsync ();
            }

            protected override bool TryComputeLength (out long length)
            {
                length = -1;
                return false;
            }
        }

        sealed class HttpResponseStream : Stream
        {
            readonly Stream baseStream;

            public HttpResponseStream (Stream baseStream)
                => this.baseStream = baseStream
                    ?? throw new ArgumentNullException (nameof (baseStream));

            public override bool CanRead => baseStream.CanRead;
            public override bool CanSeek => baseStream.CanSeek;
            public override bool CanWrite => baseStream.CanWrite;
            public override long Length => baseStream.Length;

            public override long Position {
                get => baseStream.Position;
                set => baseStream.Position = value;
            }

            public override long Seek (long offset, SeekOrigin origin)
                => baseStream.Seek (offset, origin);

            public override void SetLength (long value)
                => baseStream.SetLength (value);

            public override int Read (byte [] buffer, int offset, int count)
            {
                var read = baseStream.Read (buffer, offset, count);
                if (read == 0)
                    throw new EndOfStreamException ();
                return read;
            }

            public override async Task<int> ReadAsync (
                byte [] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken)
            {
                var read = await baseStream
                    .ReadAsync (buffer, offset, count, cancellationToken)
                    .ConfigureAwait (false);
                if (read == 0)
                    throw new EndOfStreamException ();
                return read;
            }

            public override void Write (byte [] buffer, int offset, int count)
                => baseStream.Write (buffer, offset, count);

            public override Task WriteAsync (
                byte [] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken)
                => baseStream.WriteAsync (buffer, offset, count, cancellationToken);

            public override void Flush ()
                => baseStream.Flush ();

            public override Task FlushAsync (CancellationToken cancellationToken)
                => baseStream.FlushAsync (cancellationToken);

            public override Task CopyToAsync (
                Stream destination,
                int bufferSize,
                CancellationToken cancellationToken)
                => baseStream.CopyToAsync (destination, bufferSize, cancellationToken);

            public override void Close ()
                => baseStream.Close ();

            protected override void Dispose (bool disposing)
            {
                if (disposing)
                    baseStream.Dispose ();
            }
        }
    }
}