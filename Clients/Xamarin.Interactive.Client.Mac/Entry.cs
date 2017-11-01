//
// Entry.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Linq;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
	static class Entry
	{
		static int Main (string [] args)
		{
			NSApplication.Init ();

			if (args.Length > 0 && args [0] == "cli") {
				ObjCRuntime.Runtime.ChangeToOriginalWorkingDirectory ();

				var exitCode = CommandLineTool.Entry.Run (
					args.Skip (1).ToArray (),
					out var shouldExit);

				if (shouldExit)
					return exitCode;

				args = Array.Empty<string> ();
			}

			NSApplication.Main (args);

			return 0;
		}
	}
}