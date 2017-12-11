//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;

[assembly: AgentProcessRegistration (
    "console-netcore",
    typeof (DotNetCoreAgentProcess))]

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class DotNetCoreAgentProcess : IAgentProcess
    {
        const string TAG = nameof (DotNetCoreAgentProcess);

        Process workbookAppProcess;

        public IWorkbookAppInstallation WorkbookApp { get; }

        public event EventHandler UnexpectedlyTerminated;

        public DotNetCoreAgentProcess (IWorkbookAppInstallation workbookApp)
            => WorkbookApp = workbookApp ?? throw new ArgumentNullException (nameof (workbookApp));

        public Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken)
        {
            var processName = WorkbookApp.AppPath;
            var processArgs = String.Join (" ", identifyAgentRequest.ToCommandLineArguments ());

            workbookAppProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = processName,
                    Arguments = processArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            workbookAppProcess.Exited += HandleWorkbookAppProcessExited;

            workbookAppProcess.Start ();

            return Task.CompletedTask;
        }

        void HandleWorkbookAppProcessExited (object sender, EventArgs e)
        {
            workbookAppProcess.Exited -= HandleWorkbookAppProcessExited;
            UnexpectedlyTerminated?.Invoke (this, EventArgs.Empty);
        }

        public Task TerminateAgentProcessAsync ()
        {
            if (workbookAppProcess != null) {
                try {
                    workbookAppProcess.Exited -= HandleWorkbookAppProcessExited;
                    workbookAppProcess.Kill ();
                    workbookAppProcess.WaitForExit ();
                } catch (Exception e) {
                    Log.Error (TAG, e);
                }
                workbookAppProcess = null;
            }

            return Task.CompletedTask;
        }
    }
}