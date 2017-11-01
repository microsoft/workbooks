//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Messages;

[assembly: AgentProcessManager.Registration (
    "test",
    typeof (AgentProcessManager<Xamarin.Interactive.Tests.TestAgentProcess>))]

namespace Xamarin.Interactive.Tests
{
    static class WorkbookAppInstallationExtensions
    {
        public static async Task<AgentProcessTicket> RequestTestTicketAsync (
            this WorkbookAppInstallation workbookApp,
            Action disconnectedHandler = null,
            CancellationToken cancellationToken = default (CancellationToken))
            => (AgentProcessTicket)await workbookApp.RequestAgentTicketAsync (
                new ClientSessionUri (AgentType.Test, ClientSessionKind.Workbook),
                new TestMessageService (),
                disconnectedHandler ?? (() => { }),
                cancellationToken);
    }

    sealed class TestAgentProcess : IAgentProcess
    {
        TestAgent agent;

        public AgentClient AgentClient { get; private set; }
        public WorkbookAppInstallation WorkbookApp { get; }

        public event EventHandler UnexpectedlyTerminated;

        public TestAgentProcess (WorkbookAppInstallation workbookApp)
            => WorkbookApp = workbookApp ?? throw new ArgumentNullException (nameof (workbookApp));

        static SynchronizationContext backupSynchronizationContext;
        static SynchronizationContext BackupSynchronizationContext
        {
            get {
                if (backupSynchronizationContext != null)
                    return backupSynchronizationContext;

                var ctx = new SingleThreadSynchronizationContext ();
                backupSynchronizationContext = ctx;

                new Thread (() => {
                    ctx.RunOnCurrentThread ();
                }) { IsBackground = true }.Start ();

                return backupSynchronizationContext;
            }
        }

        public Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken)
        {
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext (BackupSynchronizationContext);
            agent = new TestAgent (identifyAgentRequest);
            agent.Stopped += UnexpectedlyTerminated;
            agent.Start ();
            return Task.CompletedTask;
        }

        public Task AssociateAgentAsync (
            AgentIdentity agentIdentity,
            AgentClient agentClient,
            CancellationToken cancellationToken)
        {
            AgentClient = agentClient;
            return Task.CompletedTask;
        }

        public Task TerminateAgentProcessAsync ()
        {
            if (agent != null) {
                agent.Stop ();
                agent = null;
            }

            return Task.CompletedTask;
        }
    }
}