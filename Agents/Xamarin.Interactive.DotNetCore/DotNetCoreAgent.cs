//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;

using Xamarin.Interactive.CodeAnalysis;
using Xamarin.Interactive.ConsoleAgent;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.DotNetCore
{
    sealed class DotNetCoreAgent : Agent
    {
        const string TAG = nameof (DotNetCoreAgent);

        public DotNetCoreAgent ()
            => Identity = new AgentIdentity (
                AgentType.DotNetCore,
                Sdk.FromEntryAssembly (".NET Core"),
                Assembly.GetEntryAssembly ().GetName ().Name);

        protected override IdentifyAgentRequest GetIdentifyAgentRequest ()
            => IdentifyAgentRequest.FromCommandLineArguments (Environment.GetCommandLineArgs ());

        public override InspectView GetVisualTree (string hierarchyKind)
            => throw new NotSupportedException ();

        public override void LoadExternalDependencies (
            Assembly loadedAssembly,
            AssemblyDependency [] externalDependencies)
        {
            if (externalDependencies == null)
                return;

            foreach (var externalDep in externalDependencies) {
                try {
                    Log.Debug (TAG, $"Loading external dependency from {externalDep.Location}…");
                    if (MacIntegration.IsMac) {
                        // Don't do anything for now on Mac, nothing we've tried
                        // so far works. :(
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