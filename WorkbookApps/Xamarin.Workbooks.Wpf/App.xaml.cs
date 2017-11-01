//
// App.xaml.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Diagnostics;
using System.Windows;

using Xamarin.Interactive;
using Xamarin.Interactive.Core;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Wpf;

namespace Xamarin.Workbooks.Wpf
{
	public partial class App : Application
	{
		void App_OnStartup (object sender, StartupEventArgs e)
		{
			var agent = new WpfAgent (() => new MainWindow ());

			try {
				var request = IdentifyAgentRequest.FromCommandLineArguments (Environment.GetCommandLineArgs ());
				if (request.ProcessId >= 0) {
					var parentProcess = Process.GetProcessById (request.ProcessId);
					parentProcess.EnableRaisingEvents = true;
					parentProcess.Exited += (o, _) => Environment.Exit (0);
				}
			} catch (Exception ex) {
				Log.Error ("App", ex);
			}

			agent.Start (new AgentStartOptions {
				ClientSessionKind = ClientSessionKind.Workbook
			});

			DebuggingSupport.LaunchClientAppForDebugging (agent);
		}
	}
}