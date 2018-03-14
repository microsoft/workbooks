// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.CodeAnalysis.Events;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Session
{
    using static InteractiveSessionEventKind;

    public sealed class InteractiveSession : IMessageService, IDisposable
    {
        public static InteractiveSession CreateWorkbookSession ()
            => new InteractiveSession (ClientSessionKind.Workbook, null);

        internal static InteractiveSession CreateLiveInspectionSession (
            ClientSessionUri liveInspectAgentUri)
            => new InteractiveSession (
                ClientSessionKind.LiveInspection,
                liveInspectAgentUri
                    ?? throw new ArgumentNullException (nameof (liveInspectAgentUri)));

        readonly ClientSessionKind sessionKind;
        readonly ClientSessionUri liveInspectAgentUri;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource ();

        readonly Observable<InteractiveSessionEvent> events = new Observable<InteractiveSessionEvent> ();
        public IObservable<InteractiveSessionEvent> Events => events;

        bool isDisposed;

        InteractiveSessionState state = InteractiveSessionState.Create ();
        InteractiveSessionState State {
            get {
                CheckDisposed ();
                return state;
            }

            set => state = value;
        }

        internal EvaluationService EvaluationService => State.EvaluationService.service;
        internal IWorkspaceService WorkspaceService => State.WorkspaceService;
        internal PackageManagerService PackageManagerService => State.PackageManagerService;

        InteractiveSession (
            ClientSessionKind sessionKind,
            ClientSessionUri liveInspectAgentUri)
        {
            this.sessionKind = sessionKind;
            this.liveInspectAgentUri = liveInspectAgentUri;
        }

        void CheckDisposed ()
        {
            if (isDisposed)
                throw new ObjectDisposedException (nameof (InteractiveSession));
        }

        public void Dispose ()
        {
            CheckDisposed ();
            isDisposed = true;
            State = default;
            events.Observers.OnCompleted ();
            cancellationTokenSource.Cancel ();
            cancellationTokenSource.Dispose ();
        }

        void PostEvent (InteractiveSessionEventKind eventKind, object data = null)
            => events.Observers.OnNext (new InteractiveSessionEvent (eventKind, data));

        CancellationToken GetCancellationToken (CancellationToken cancellationToken = default)
            => cancellationTokenSource.Token.LinkWith (cancellationToken);

        public async Task InitializeAsync (
            InteractiveSessionDescription sessionDescription,
            CancellationToken cancellationToken = default)
        {
            cancellationToken = GetCancellationToken (cancellationToken);

            State = State.WithSessionDescription (sessionDescription);

            PostEvent (ConnectingToAgent);

            await InitializeAgentConnectionAsync (
                cancellationToken).ConfigureAwait (false);

            PostEvent (InitializingWorkspace);

            var workspaceConfiguration = await WorkspaceConfiguration.CreateAsync (
                State.AgentConnection,
                sessionKind,
                cancellationToken).ConfigureAwait (false);

            var workspaceService = await WorkspaceServiceFactory.CreateWorkspaceServiceAsync (
                sessionDescription.LanguageDescription,
                workspaceConfiguration,
                cancellationToken).ConfigureAwait (false);

            var evaluationService = new EvaluationService (
                workspaceService,
                sessionDescription.EvaluationEnvironment
                    ?? new EvaluationEnvironment (null));

            evaluationService.NotifyAgentConnected (state.AgentConnection);

            PackageManagerService packageManagerService = null;

            if (State.WorkbookApp?.Sdk != null) {
                packageManagerService = new PackageManagerService (
                    workspaceService.Configuration.DependencyResolver,
                    evaluationService,
                    PackageManager_GetAgentConnectionHandler);

                await packageManagerService.InitializeAsync (
                    state.WorkbookApp.Sdk,
                    state.PackageManagerService?.GetInstalledPackages (),
                    cancellationToken).ConfigureAwait (false);
            }

            State.EvaluationService.eventObserver?.Dispose ();

            State = State.WithServices (
                workspaceService,
                (evaluationService, evaluationService.Events.Subscribe (
                    new Observer<ICodeCellEvent> (OnEvaluationServiceEvent))),
                packageManagerService);

            PostEvent (Ready);
        }

        void OnEvaluationServiceEvent (ICodeCellEvent codeCellEvent)
            => PostEvent (Evaluation, codeCellEvent);

        #region Agent Connection

        async Task<IAgentConnection> PackageManager_GetAgentConnectionHandler (
            bool refreshForAgentIntegration,
            CancellationToken cancellationToken)
        {
            if (refreshForAgentIntegration) {
                State = state.WithAgentConnection (
                    await State
                        .AgentConnection
                        .RefreshFeaturesAsync ().ConfigureAwait (false));

                PostEvent (AgentFeaturesUpdated);
            }

            return State.AgentConnection;
        }

        public void TerminateAgentConnection ()
            => State = State.WithAgentConnection (State.AgentConnection.TerminateConnection ());

        async Task InitializeAgentConnectionAsync (CancellationToken cancellationToken = default)
        {
            void ResetAgentConnection ()
            {
                var agentType = AgentType.Unknown;

                if (State.AgentConnection != null) {
                    agentType = State.AgentConnection.Type;
                    ((IDisposable)State.AgentConnection).Dispose ();
                }

                State.EvaluationService.service?.NotifyAgentDisconnected ();

                State = State.WithAgentConnection (new AgentConnection (agentType));
            }

            void HandleAgentDisconnected ()
            {
                ResetAgentConnection ();
                EvaluationService?.OutdateAllCodeCells ();
            }

            ResetAgentConnection ();

            if (State.AgentConnection?.IsConnected == true)
                TerminateAgentConnection ();

            WorkbookAppInstallation workbookApp = null;

            if (sessionKind == ClientSessionKind.Workbook)
                workbookApp = WorkbookAppInstallation.LookupById (
                    State.SessionDescription.TargetPlatformIdentifier);

            var agentConnection = await State.AgentConnection.ConnectAsync (
                workbookApp,
                liveInspectAgentUri,
                this,
                HandleAgentDisconnected,
                GetCancellationToken (cancellationToken)).ConfigureAwait (false);

            await agentConnection
                .Api
                .SetLogLevelAsync (Log.GetLogLevel ()).ConfigureAwait (false);

            State = State.WithAgentConnection (
                agentConnection,
                workbookApp);
        }

        #endregion

        #region IMessageService

        bool IMessageService.CanHandleMessage (Message message)
            => true;

        Message IMessageService.PushMessage (Message message)
            => message;

        void IMessageService.DismissMessage (int messageId)
        {
        }

        #endregion
    }
}