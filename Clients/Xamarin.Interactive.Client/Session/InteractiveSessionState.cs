//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.NuGet;

namespace Xamarin.Interactive.Session
{
    struct InteractiveSessionState
    {
        public static InteractiveSessionState Create ()
            => new InteractiveSessionState ();

        public InteractiveSessionDescription SessionDescription { get; }
        public IWorkspaceService WorkspaceService { get; }
        public (EvaluationService service, IDisposable eventObserver) EvaluationService { get; }
        public PackageManagerService PackageManagerService { get; }
        public IWorkbookAppInstallation WorkbookApp { get; }
        public AgentConnection AgentConnection { get; }

        InteractiveSessionState (
            InteractiveSessionDescription sessionDescription,
            IWorkspaceService workspaceService,
            (EvaluationService service, IDisposable eventObserver) evaluationService,
            PackageManagerService packageManagerService,
            IWorkbookAppInstallation workbookApp,
            AgentConnection agentConnection)
        {
            SessionDescription = sessionDescription;
            WorkspaceService = workspaceService;
            EvaluationService = evaluationService;
            PackageManagerService = packageManagerService;
            WorkbookApp = workbookApp;
            AgentConnection = agentConnection;
        }

        public InteractiveSessionState WithSessionDescription (
            InteractiveSessionDescription sessionDescription)
            => new InteractiveSessionState (
                sessionDescription,
                WorkspaceService,
                EvaluationService,
                PackageManagerService,
                WorkbookApp,
                AgentConnection);

        public InteractiveSessionState WithAgentConnection (
            AgentConnection agentConnection,
            IWorkbookAppInstallation workbookApp = null)
            => new InteractiveSessionState (
                SessionDescription,
                WorkspaceService,
                EvaluationService,
                PackageManagerService,
                workbookApp,
                agentConnection);

        public InteractiveSessionState WithServices (
            IWorkspaceService workspaceService,
            (EvaluationService service, IDisposable eventObserver) evaluationService,
            PackageManagerService packageManagerService)
            => new InteractiveSessionState (
                SessionDescription,
                workspaceService,
                evaluationService,
                packageManagerService,
                WorkbookApp,
                AgentConnection);
    }
}