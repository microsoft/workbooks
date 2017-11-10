//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

using Android.App;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Android;

namespace Xamarin
{
    public static class InspectorSupport
    {
        static readonly SynchronizationContext mainContext = Application.SynchronizationContext;
        static Timer timer;
        static readonly ActivityTrackerWrapper activityTracker = new ActivityTrackerWrapper ();
        static Agent agent;

        static void CreateAgent (AgentStartOptions startOptions)
        {
            mainContext.Post (
                s => {
                    timer.Dispose ();
                    agent = new AndroidAgent (activityTracker).Start (startOptions);
                },
                null);
        }

        internal static void StartBreakdance ()
        {
            timer = new Timer (
                s => {
                    timer.Change (Timeout.Infinite, Timeout.Infinite);
                    mainContext.Post (st => {
                        BreakdanceStep ();
                        timer.Change (1000, Timeout.Infinite);
                    }, null);
                },
                null,
                1000,
                Timeout.Infinite);
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
        }
    }
}
