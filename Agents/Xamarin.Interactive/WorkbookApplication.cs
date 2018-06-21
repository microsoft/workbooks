//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive
{
    static class WorkbookApplication
    {
        // Usage: Xamarin.Interactive.Console.exe {IdentifyAgentRequest args}
        // Passing no arguments is supported for debug scenarios.
        public static void RunAgentOnCurrentThread<TAgent> (
            Action<TAgent> integrateWithAgentHandler = null)
            where TAgent : Agent, new ()
        {
            var ctx = new SingleThreadSynchronizationContext ();
            SynchronizationContext.SetSynchronizationContext (ctx);

            var agent = new TAgent ();
            integrateWithAgentHandler?.Invoke (agent);

            try {
                var request = IdentifyAgentRequest.FromCommandLineArguments (Environment.GetCommandLineArgs ());
                if (request != null && request.ProcessId >= 0) {
                    if (HostEnvironment.OS == HostOS.macOS) {
                        new Thread (() => {
                            MonoTouch.Hosting.ProcessMonitor.WaitPid (request.ProcessId);
                            Environment.Exit (0);
                        }) { IsBackground = true }.Start ();
                    } else {
                        var parentProcess = Process.GetProcessById (request.ProcessId);
                        parentProcess.EnableRaisingEvents = true;
                        parentProcess.Exited += (o, e) => Environment.Exit (0);
                    }
                }
            } catch (Exception e) {
                Log.Error ("Main", e);
            }

            agent.Start (new AgentStartOptions {
                ClientSessionKind = ClientSessionKind.Workbook
            });

            DebuggingSupport.LaunchClientAppForDebugging (agent);

            ctx.RunOnCurrentThread ();
        }
    }
}
