//
// MergePlist.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

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
