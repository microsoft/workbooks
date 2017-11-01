//
// MainClass.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.ConsoleAgent
{
	class MainClass
	{
		// Usage: Xamarin.Interactive.Console.exe {IdentifyAgentRequest args}
		// Passing no arguments is supported for debug scenarios.
		public static void Main ()
		{
			var ctx = new SingleThreadSynchronizationContext ();
			SynchronizationContext.SetSynchronizationContext (ctx);

			var agent = new ConsoleAgent ();

			MacIntegration.Integrate (agent);

			try {
				var request = IdentifyAgentRequest.FromCommandLineArguments (Environment.GetCommandLineArgs ());
				if (request != null && request.ProcessId >= 0) {
					if (Environment.OSVersion.Platform == PlatformID.Unix) {
						new Thread (() => {
							MonoTouch.Hosting.ProcessMonitor.WaitPid (request.ProcessId);
							Environment.Exit (0);
						}) { IsBackground = true }.Start ();
					} else {
						var parentProcess = Process.GetProcessById (request.ProcessId);
						parentProcess.EnableRaisingEvents = true;
						parentProcess.Exited += (o, e) => Environment.Exit (0);
					}
				}
			} catch (Exception e) {
				Log.Error ("Main", e);
			}

			agent.Start (new AgentStartOptions {
				ClientSessionKind = ClientSessionKind.Workbook
			});

			DebuggingSupport.LaunchClientAppForDebugging (agent);

			ctx.RunOnCurrentThread ();
		}
	}
}
