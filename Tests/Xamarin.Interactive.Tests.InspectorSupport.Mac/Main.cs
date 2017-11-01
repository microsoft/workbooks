//
// Main.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

using AppKit;

namespace Xamarin.Interactive.Tests.InspectorSupport.Mac
{
    static class MainClass
    {
        static readonly BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
        static Type inspectorSupportType;

        [DllImport ("libc")]
        static extern void exit (int status);

        static void Main (string [] args)
        {
            new Timer (state => {
                Console.WriteLine ("Timeout reached. Inspector integration failed.");
                // Environment.Exit does not work well with Cocoa
                exit (1);
            }, null, TimeSpan.FromMinutes (1), Timeout.InfiniteTimeSpan);

            StartInspectorIntegration ();

            NSApplication.Init ();
            NSApplication.Main (args);
        }

        static void StartInspectorIntegration ()
        {
            var buildRoot = Assembly.GetExecutingAssembly ().Location;
            for (int i = 0; i < 8; i++)
                buildRoot = Path.GetDirectoryName (buildRoot);

            InteractiveInstallation.InitializeDefault (true, buildRoot);

            var agentAssemblyPath = InteractiveInstallation.Default.LocateAgentAssembly (
                AgentType.MacMobile);

            Console.WriteLine ("Injecting Agent Assembly");
            Console.WriteLine ($"  path:  {agentAssemblyPath}");
            Console.WriteLine ($"  mtime: {File.GetLastWriteTime (agentAssemblyPath)}");

            inspectorSupportType = Assembly
                .LoadFrom (agentAssemblyPath)
                .GetType ("Xamarin.InspectorSupport");

            inspectorSupportType
                .GetField ("AgentStartedHandler", bindingFlags)
                .SetValue (null, new Action<object> (AgentStarted));

            inspectorSupportType
                .GetMethod ("Start", bindingFlags)
                .Invoke (null, null);
        }

        static void AgentStarted (object agent)
        {
            Console.WriteLine ($"AgentStarted invoked: {agent}. Success.");
            exit (0);
        }
    }
}