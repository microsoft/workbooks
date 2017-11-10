//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Xamarin.Interactive;
using Xamarin.Interactive.Core;

#if IOS
using UnifiedAgent = Xamarin.Interactive.iOS.iOSAgent;
#elif MAC
using UnifiedAgent = Xamarin.Interactive.Mac.MacAgent;
#endif

namespace Xamarin
{
    public static class InspectorSupport
    {
        static IntPtr breakdanceTimerSource;
        static Agent agent;

        internal static Action<string> AgentStartedHandler;

        internal static void StartBreakdance ()
        {
            breakdanceTimerSource = Dispatch.ScheduleRepeatingTimer (TimeSpan.FromSeconds (1),
                userdata => BreakdanceStep ());
        }

        static void StopBreakdance ()
        {
            if (breakdanceTimerSource != IntPtr.Zero) {
                Dispatch.Cancel (breakdanceTimerSource);
                breakdanceTimerSource = IntPtr.Zero;
            }
        }

        static void CreateAgent (AgentStartOptions startOptions)
        {
            StopBreakdance ();

            var source = IntPtr.Zero;
            source = Dispatch.ScheduleRepeatingTimer (TimeSpan.FromSeconds (0), userdata => {
                Dispatch.Cancel (source);
                agent = new UnifiedAgent ().Start (startOptions);
            });
        }

        static void Start ()
        {
            try {
                CreateAgent (new AgentStartOptions {
                    AgentStarted = AgentStarted,
                });
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

        [MethodImpl (MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void AgentStarted (string agentConnectUri)
        {
            // This handler is only ever used by the inspector support test.
            // In normal use, the Inspector extension retrieves the URI by
            // setting a breakpoint on the AgentStarted method.
            AgentStartedHandler?.Invoke (agentConnectUri);
        }
    }
}