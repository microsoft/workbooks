//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Wpf;

namespace Xamarin
{
    public static partial class InspectorSupport
    {
        static string connectionInfoPipeHandle;

        static partial void CreateAgent (AgentStartOptions startOptions)
        {
            // NOTE: This needs to be called from the main thread
            agent = new WpfAgent ().Start (startOptions);
        }

        public static void Start (string connectionInfoPipeHandle)
        {
            InspectorSupport.connectionInfoPipeHandle = connectionInfoPipeHandle;

            Start ();
        }

        internal static void AgentStarted (string agentConnectUri)
        {
            if (String.IsNullOrEmpty (connectionInfoPipeHandle))
                return;

            Task.Run (() => {
                try {
                    using (var pipeClient = new NamedPipeClientStream (
                        ".", connectionInfoPipeHandle, PipeDirection.Out)) {
                        pipeClient.Connect (500);

                        using (var sw = new StreamWriter (pipeClient)) {
                            sw.AutoFlush = true;
                            sw.WriteLine (agentConnectUri);
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine (e);
                }
            });
        }
    }
}
