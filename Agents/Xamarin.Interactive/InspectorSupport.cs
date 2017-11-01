//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

using Xamarin.Interactive.Core;

namespace Xamarin
{
    public static partial class InspectorSupport
    {
        static Agent agent;

        static partial void CreateAgent (AgentStartOptions startOptions);

        internal static Action<object> AgentStartedHandler;

        #if DEBUG
        public static void Start (AgentStartOptions startOptions = null) => CreateAgent (startOptions);
        #endif

        static void Start ()
        {
            try {
                CreateAgent (null);
            } catch (Exception e) {
                Console.Error.WriteLine (e);
            }
        }

        internal static void Stop ()
        {
            agent?.Dispose ();
            agent = null;
        }

        [MethodImpl (MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void BreakdanceStep ()
        {
        }

        #if !WPF

        [MethodImpl (MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void AgentStarted (string agentConnectUri)
        {
        }

        #endif
    }
}