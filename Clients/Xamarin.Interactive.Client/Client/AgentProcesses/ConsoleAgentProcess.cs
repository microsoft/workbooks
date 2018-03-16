//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.ProcessControl;

using Xamarin.Interactive.Client.AgentProcesses;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.I18N;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Messages;

[assembly: AgentProcessRegistration (
    "console",
    typeof (ConsoleAgentProcess))]

namespace Xamarin.Interactive.Client.AgentProcesses
{
    sealed class ConsoleAgentProcess : IAgentProcess
    {
        const string TAG = nameof(ConsoleAgentProcess);
        const string monoPath = "/Library/Frameworks/Mono.framework/Commands/mono64";

        Process workbookAppProcess;

        public IWorkbookAppInstallation WorkbookApp { get; }

        public event EventHandler UnexpectedlyTerminated;

        public ConsoleAgentProcess (IWorkbookAppInstallation workbookApp)
            => WorkbookApp = workbookApp ?? throw new ArgumentNullException (nameof (workbookApp));

        public Task StartAgentProcessAsync (
            IdentifyAgentRequest identifyAgentRequest,
            IMessageService messageService,
            CancellationToken cancellationToken)
        {
            var useMono = HostEnvironment.OS != HostOS.Windows;

            if (useMono && !File.Exists (monoPath))
                throw new UserPresentableException (
                    Catalog.GetString ("Xamarin install missing."));

            string processName;

            var processArgs = ProcessArguments.Create (
                identifyAgentRequest.ToCommandLineArguments ());

            if (useMono) {
                processName = monoPath;
                processArgs = processArgs.Insert (0, WorkbookApp.AppPath);
            } else {
                processName = WorkbookApp.AppPath;
            }

            workbookAppProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = processName,
                    Arguments = processArgs.ToString (),
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