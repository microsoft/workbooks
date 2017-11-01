//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public sealed class CommonGitInfo : Task
    {
        public string MostRecentTagRegex { get; set; }
        public string MinRevision { get; set; }
        public string MaxRevision { get; set; }
        public bool SetMinRevisionFromMostRecentTagRegex { get; set; }

        [Output]
        public string MinRevisionLong { get; private set; }

        [Output]
        public string MinRevisionShort { get; private set; }

        [Output]
        public string MinRevisionTimestamp { get; private set; }

        [Output]
        public string MaxRevisionLong { get; private set; }

        [Output]
        public string MaxRevisionShort { get; private set; }

        [Output]
        public string MaxRevisionTimestamp { get; private set; }

        [Output]
        public string MostRecentTag { get; private set; }

        [Output]
        public string MostRecentTagMatchingRegex { get; private set; }

        [Output]
        public int MinMaxCommitDistance { get; private set; }

        public override bool Execute ()
        {
            if (string.IsNullOrEmpty (MaxRevision))
                MaxRevision = "HEAD";

            MaxRevisionLong = GetRevision (MaxRevision);
            MaxRevisionShort = MaxRevisionLong.Substring (0, 7);
            MaxRevisionTimestamp = GetCommitTimestamp (MaxRevision)
                .ToString (CultureInfo.InvariantCulture);

            MostRecentTag = GetMostRecentTag ();

            if (!string.IsNullOrEmpty (MostRecentTag) && !string.IsNullOrEmpty (MostRecentTagRegex)) {
                // I hope no one needs to match a literal '/' in their tags...
                // Convert these back to '\' since they are converted to '/' by
                // MSBuild directly.
                MostRecentTagRegex = MostRecentTagRegex.Replace ('/', '\\');
                MostRecentTagMatchingRegex = GetMostRecentTagMatching (new Regex (MostRecentTagRegex));
                if (string.IsNullOrEmpty (MostRecentTagMatchingRegex)) {
                    Log.LogError ($"No tags were matched with the regex: {MostRecentTagRegex}");
                    return false;
                }

                if (SetMinRevisionFromMostRecentTagRegex &&
                    !string.IsNullOrEmpty (MostRecentTagMatchingRegex))
                    MinRevision = GetRevision (MostRecentTagMatchingRegex);
            }

            if (string.IsNullOrEmpty (MinRevision))
                MinRevision = Exec
                    .Run ("git", "rev-list", "--max-parents=0", "HEAD")
                    .FirstOrDefault ();

            MinRevisionLong = MinRevision;
            if (!string.IsNullOrEmpty (MinRevision)) {
                MinRevisionShort = MinRevision.Substring (0, 7);
                MinRevisionTimestamp = GetCommitTimestamp (MinRevision)
                    .ToString (CultureInfo.InvariantCulture);
                MinMaxCommitDistance = GetCommitDistance (MinRevision, MaxRevision);
            }

            return true;
        }

        public static string GetRevision (string revision)
            => Exec.Run ("git", "rev-parse", revision).FirstOrDefault ();

        public static int GetCommitTimestamp (string revision)
            => int.Parse (Exec.Run (
                "git",
                "log",
                "--no-color",
                "--first-parent",
                "-n1",
                "--pretty=format:%ct",
                revision).First ().Trim ());

        public static int GetCommitDistance (string revision1, string revision2)
        {
            if (revision1 == null || revision2 == null)
                return 0;

            return Exec.Run ("git", "log", "--oneline", $"{revision1}..{revision2}").Count;
        }

        public static IEnumerable<string> GetTagsByDateOrder ()
            => Exec.Run ("git",
                "for-each-ref",
                "--sort=taggerdate",
                "--format", "%(refname)",
                "refs/tags");

        public static string GetMostRecentTag ()
            => GetTagsByDateOrder ().LastOrDefault ();

        public static string GetMostRecentTagMatching (Regex regex)
            => GetTagsByDateOrder ().LastOrDefault (regex.IsMatch);

        public static string GetSymbolicRef (string revision)
            => Exec.Run ("git", "symbolic-ref", revision).FirstOrDefault ()?.Replace (
                "refs/heads/",
                string.Empty);
    }
}