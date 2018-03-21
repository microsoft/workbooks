//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using Xamarin.Interactive.CodeAnalysis.Resolving;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.ConsoleAgent
{
    sealed class ConsoleAgent : Agent
    {
        const string TAG = nameof (ConsoleAgent);

        public ConsoleAgent ()
            => Identity = new AgentIdentity (
                AgentType.Console,
                Sdk.FromEntryAssembly ("Console"),
                Assembly.GetEntryAssembly ().GetName ().Name);

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
            => IdentifyAgentRequest.FromCommandLineArguments (Environment.GetCommandLineArgs ());

        public override InspectView GetVisualTree (string hierarchyKind)
        {
            throw new NotSupportedException ();
        }

        public override void LoadExternalDependencies (
            Assembly loadedAssembly,
            AssemblyDependency [] externalDependencies)
        {
            if (externalDependencies == null)
                return;

            // On Mono platforms, we can't do anything until we've loaded the assembly, because we need
            // to insert things specifically into its DllMap.
            if (MacIntegration.IsMac && loadedAssembly == null)
                return;

            foreach (var externalDep in externalDependencies) {
                try {
                    Log.Debug (TAG, $"Loading external dependency from {externalDep.Location}.");
                    if (MacIntegration.IsMac) {
                        MonoSupport.AddDllMapEntries (loadedAssembly, externalDep);
                    } else {
                        WindowsSupport.LoadLibrary (externalDep.Location);
                    }
                } catch (Exception e) {
                    Log.Error (TAG, "Could not load external dependency.", e);
                }
            }
        }
    }
}