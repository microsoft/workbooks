//
// Exec.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Xamarin
{
	static class Exec
	{
		public static List<string> Run (string fileName, params string [] arguments)
			=> Run (fileName, (IEnumerable<string>)arguments);

		public static List<string> Run (string fileName, IEnumerable<string> arguments)
		{
			var proc = Process.Start (new ProcessStartInfo {
				FileName = fileName,
				Arguments = String.Join (" ", arguments.Select (QuoteArgument)),
				UseShellExecute = false,
				RedirectStandardError = false,
				RedirectStandardOutput = true
			});

			var output = new List<string> ();

			string line;
			while ((line = proc.StandardOutput.ReadLine ()) != null)
				output.Add (line);

			proc.WaitForExit ();

			if (proc.ExitCode != 0)
				throw new Exception ($"'{proc.StartInfo.FileName} " +
					$"{proc.StartInfo.Arguments}' exited {proc.ExitCode}");

			return output;
		}

		public static string QuoteArgument (string argument)
		{
			if (String.IsNullOrEmpty (argument))
				return argument;

			StringBuilder builder = null;

			for (int i = 0; i < argument.Length; i++) {
				var c = argument [i];
				if (Char.IsWhiteSpace (c) || c == '\b' || c == '"' || c == '\\') {
					if (builder == null) {
						builder = new StringBuilder (argument.Length + 8);
						builder.Append ('"');
						builder.Append (argument.Substring (0, i));
					}

					if (c == '"' || c == '\\')
						builder.Append ('\\');
				}

				if (builder != null)
					builder.Append (c);
			}

			if (builder == null)
				return argument;

			builder.Append ('"');
			return builder.ToString ();
		}
	}
}
