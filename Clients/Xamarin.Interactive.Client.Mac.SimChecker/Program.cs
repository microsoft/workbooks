//
// Program.cs
//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xamarin.Interactive.MTouch;

namespace Xamarin.Interactive.Mac.SimChecker
{
	public class Program
	{
		public static void Main (string[] args)
		{
			if (args.Length == 1 && args [0] == "--version") {
				Console.WriteLine (BuildInfo.VersionString);
				Environment.Exit (0);
				return;
			}

			string sdkRoot;
			try {
				sdkRoot = MTouchSdkTool.GetXcodeSdkRoot ();
			} catch (Exception e) {
				Console.Error.WriteLine (e.Message);
				Environment.Exit (100); // Xcode not configured in XS or not installed at /Applications/Xcode.app
				return;
			}

			var xcodeVersion = MTouchSdkTool.GetXcodeVersion (sdkRoot);
			if (xcodeVersion < MTouchSdkTool.RequiredMinimumXcodeVersion) {
				Environment.Exit (105); // Xcode too old
				return;
			}

			MTouchListSimXml mtouchList;
			try {
				mtouchList = MTouchSdkTool.MtouchListSimAsync (sdkRoot).Result;
			} catch (Exception e) {
				e = (e as AggregateException)?.InnerExceptions?.FirstOrDefault () ?? e;
				Console.Error.WriteLine (e.Message);
				if (e is FileNotFoundException)
					Environment.Exit (101); // mlaunch (Xamarin Studio) not installed
				else
					Environment.Exit (102); // Error running mlaunch
				return;
			}

			IEnumerable<MTouchListSimXml.SimDeviceElement> compatibleDevices;
			try {
				compatibleDevices = MTouchSdkTool.GetCompatibleDevices (mtouchList);
			} catch (Exception e) {
				Console.Error.WriteLine (e.Message);
				Environment.Exit (103); // Invalid mlaunch output
				return;
			}

			var firstCompatibleDevice = compatibleDevices?.FirstOrDefault ();
			if (firstCompatibleDevice == null) {
				Console.Error.WriteLine ("No compatible simulator devices installed");
				Environment.Exit (104); // No compatible sim listed by mlaunch
			}

			Console.WriteLine ($"UDID: {firstCompatibleDevice.UDID}");
		}
	}
}
