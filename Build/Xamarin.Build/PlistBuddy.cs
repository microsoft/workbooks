//
// PlistBuddy.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xamarin
{
    enum PlistBuddyVerb
    {
        Print,
        Remove,
        Set,
        Merge
    }

    class PlistBuddyCommand
    {
        public PlistBuddyVerb Verb { get; set; }
        public string [] VerbArguments { get; set; }
    }

    static class PlistBuddy
    {
        const string PlistBuddyPath = "/usr/libexec/PlistBuddy";

        public static List<string> Run (string targetPlistPath,
            PlistBuddyVerb verb,
            params string [] verbArguments)
            => Run (targetPlistPath, new [] {
                new PlistBuddyCommand {
                    Verb = verb,
                    VerbArguments = verbArguments
                }
            });

        public static List<string> Run (string targetPlistPath,
            IEnumerable<PlistBuddyCommand> commands)
        {
            var args = new List<string> ();
            foreach (var command in commands) {
                var commandArgs = String.Join (" ", command.VerbArguments.Select (Exec.QuoteArgument));
                args.Add ("-c");
                args.Add ($"{command.Verb} {commandArgs}");
            }
            args.Add (targetPlistPath);

            return Exec.Run (PlistBuddyPath, args);
        }
    }
}
