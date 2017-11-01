//
// Authors:
//   Mauro Agnoletti <mauro.agnoletti@gmail.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Xamarin.Interactive.Client.AgentProcesses
{
    interface ISshCommands : IDisposable
    {
        Task<string> GetHomeDirectoryAsync ();

        void ForwardPort (int boundPort, int port, bool remoteForward = false);
    }
}