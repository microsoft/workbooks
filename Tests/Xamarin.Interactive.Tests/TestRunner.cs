//
// TestRunner.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using GuiUnit;
using GuiUnitNg;

using Xamarin.Interactive.Core;

namespace Xamarin.Interactive.Tests
{
	public static class TestRunner
	{
		public static void Execute (
			Action<Action> dispatcher,
			IEnumerable<string> arguments)
		{
			WorkbookAppInstallation.RegisterProcessManagers (typeof (TestRunner).Assembly);

			AgentIdentificationManager.DefaultTimeout = TimeSpan.FromSeconds (5);

			ProcessControl.Exec.Log += (sender, e) => {
				if (e.ExitCode == null)
					Console.WriteLine ($"Exec[{e.ExecId}]: {e.Arguments}");
				else
					Console.WriteLine ($"Exec[{e.ExecId}]: exited {e.ExitCode}");
			};

			var args = new List<string> ();

			args.AddRange (new string[] {
				"-labels=All",
				"-noheader",
				"-workers=1"
			});

			args.AddRange (arguments.Where (a => !a.StartsWith ("-psn_", StringComparison.Ordinal)));

			Environment.CurrentDirectory = new FilePath (
				typeof (TestRunner).Assembly.Location).ParentDirectory;

			TextRunner.MainLoop = new MainLoopIntegration (dispatcher);

			Console.WriteLine (string.Join (" ", args));

			new TextRunner ().Execute (args.ToArray ());
		}

		sealed class MainLoopIntegration : IMainLoopIntegration
		{
			readonly Action<Action> dispatcher;

			public MainLoopIntegration (Action<Action> dispatcher)
				=> this.dispatcher = dispatcher
					?? throw new ArgumentNullException (nameof (dispatcher));

			public void InvokeOnMainLoop (InvokerHelper helper)
				=> dispatcher (helper.Invoke);

			[DllImport ("/usr/lib/libSystem.dylib")]
			static extern void _exit (int exitCode);

			public void Shutdown (int exitCode)
			{
				try {
					// HACK: Work around https://bugzilla.xamarin.com/show_bug.cgi?id=52604
					_exit (exitCode);
				} catch (DllNotFoundException) {
				} catch (EntryPointNotFoundException) {
				}

				Environment.Exit (exitCode);
			}

			public void InitializeToolkit ()
			{
			}

			public void RunMainLoop ()
			{
			}
		}
	}
}