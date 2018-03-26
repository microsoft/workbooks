//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
    public class MergePlists : Task
    {
        [Required]
        public string SourcePlist { get; set; }

        [Required]
        public string TargetPlist { get; set; }

        public override bool Execute ()
        {
            Log.LogMessage (MessageImportance.Normal, "MergePlists Task");
            Log.LogMessage (MessageImportance.Normal, $"  SourcePlist: {SourcePlist}");
            Log.LogMessage (MessageImportance.Normal, $"  TargetPlist: {TargetPlist}");

            PlistBuddy.Run (TargetPlist,
                PlistBuddyVerb.Merge,
                SourcePlist);

            return true;
        }
    }
}
