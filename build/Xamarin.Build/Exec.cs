//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            => Run (new ProcessStartInfo { FileName = fileName }, arguments);

        public static List<string> Run (string fileName, IEnumerable<string> arguments)
            => Run (new ProcessStartInfo { FileName = fileName }, arguments);

        public static List<string> Run (ProcessStartInfo processStartInfo, params string [] arguments)
            => Run (processStartInfo, (IEnumerable<string>)arguments);

        public static List<string> Run (ProcessStartInfo processStartInfo, IEnumerable<string> arguments)
        {
            processStartInfo.Arguments = QuoteArguments (arguments);
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardError = false;
            processStartInfo.RedirectStandardOutput = true;

            var proc = Process.Start (processStartInfo);

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

        public static string QuoteArguments (IEnumerable<string> arguments)
            => string.Join (" ", arguments.Select (QuoteArgument));

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
