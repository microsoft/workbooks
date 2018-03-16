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
using System.IO;
using System.Reflection;

using Xamarin.ProcessControl;

using Xamarin.Interactive.Client;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive
{
    static class DebuggingSupport
    {
        const string TAG = nameof (DebuggingSupport);

        public static void LaunchClientAppForDebugging (Agent agent)
        {
#if DEBUG
            if (!Debugger.IsAttached)
                return;

            var clientAssemblyLocation = Assembly.GetExecutingAssembly ().Location;

            var agentType = agent.ClientSessionUri.AgentType;

            InteractiveInstallation.InitializeDefault (null);

            var clientPath = InteractiveInstallation
                .Default
                .LocateClientApplication (agent.ClientSessionUri.SessionKind);

            if (string.IsNullOrEmpty (clientPath))
                return;

            var connectUri = new ClientSessionUri (
                agent.Identity.AgentType,
                agent.ClientSessionUri.SessionKind,
                agent.Identity.Host,
                agent.Identity.Port);

            if (HostEnvironment.OS == HostOS.macOS) {
                var executableName = Directory.GetFiles (
                    Path.Combine (clientPath, "Contents", "MacOS")) [0];
                clientPath = Path.Combine (clientPath, "Contents", "MacOS", executableName);
            }

            Exec.Log += (sender, e) => {
                if (e.ExitCode == null)
                    Log.Debug (TAG, $"Exec[{e.ExecId}] ({e.Flags}): {e.Arguments}");
                else
                    Log.Debug (TAG, $"Exec[{e.ExecId}] exited: {e.ExitCode}");
            };

            Exec.RunAsync (
                segment => Debug.WriteLine ($"!! {segment.Data.TrimEnd ()}"),
                clientPath,
                connectUri).ContinueWith (task => {
                    if (task.Exception != null)
                        Log.Error (TAG, task.Exception);
                });
#endif
        }
    }
}