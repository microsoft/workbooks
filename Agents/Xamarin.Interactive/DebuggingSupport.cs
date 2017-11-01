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
#if !NETSTANDARD2_0 && DEBUG
            if (!Debugger.IsAttached)
                return;

            var clientAssemblyLocation = Assembly.GetExecutingAssembly ().Location;

            var buildDepth = 5;
            var agentType = agent.ClientSessionUri.AgentType;

            // The Mac executable is more deeply nested, so we need to go up a little further
            // to get the base build path.
            if (agentType == AgentType.MacMobile || agentType == AgentType.MacNet45)
                buildDepth = 7;

            var buildDir = clientAssemblyLocation;
            for (var i = 0; i < buildDepth; i++)
                buildDir = Path.GetDirectoryName (buildDir);

            InteractiveInstallation.InitializeDefault (
                isMac: Environment.OSVersion.Platform == PlatformID.Unix,
                buildPath: buildDir);

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

            if (InteractiveInstallation.Default.IsMac) {
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