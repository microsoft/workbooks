//
// AppDelegate.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System.Runtime.InteropServices;

using AppKit;
using Foundation;

using Xamarin.Interactive;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Mac;

namespace Xamarin.Workbooks.Mac
{
	[Register ("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		[DllImport ("libc")]
		static extern /* pid_t -> int32_t */ int getppid ();

		public override void DidFinishLaunching (NSNotification notification)
		{
			var ppid = getppid ();

			NSWorkspace.Notifications.ObserveDidTerminateApplication ((sender, e) => {
				if (e.Application.ProcessIdentifier == ppid)
					NSApplication.SharedApplication.Terminate (e.Application);
			});

			var agent = new MacAgent ();

			agent.Start (new AgentStartOptions {
				ClientSessionKind = ClientSessionKind.Workbook
			});

			DebuggingSupport.LaunchClientAppForDebugging (agent);
		}
	}
}