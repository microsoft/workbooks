//
// Program.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Linq;

namespace ILFixer
{
	class MainClass
	{
		static int Usage (string message = null)
		{
			if (message != null) {
				Console.WriteLine ("error: {0}", message);
				Console.WriteLine ();
			}

			Console.WriteLine ("Usage: ilfixer TOOL [OPTIONS]");
			Console.WriteLine ();
			Console.WriteLine ("Available Tools");
			Console.WriteLine ();
			Console.WriteLine ("  netstandard    replaces netstandard references with mscorlib");
			Console.WriteLine ();

			return 1;
		}

		public static int Main (string [] args)
		{
			if (args.Length == 0)
				return Usage ();

			var toolArgs = args.Skip (1).ToArray ();

			switch (args [0]) {
			case "netstandard":
				return NetStandardTool.Run (toolArgs);
			case "-h":
			case "/h":
			case "/?":
			case "--help":
			case "-help":
			case "/help":
				return Usage ();
			default:
				return Usage ($"invalid tool name: {args [0]}");
			}
		}
	}
}