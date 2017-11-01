﻿//
// DeletePlistEntries.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
	public class DeletePlistEntries : Task
	{
		[Required]
		public ITaskItem [] EntriesToDelete { get; set; }

		[Required]
		public string Plist { get; set; }

		public bool IgnoreResult { get; set; }

		public override bool Execute ()
		{
			var entriesToDelete = EntriesToDelete.Select (taskItem => taskItem.ItemSpec).ToArray ();
			var entriesString = string.Join (", ", entriesToDelete);

			Log.LogMessage (MessageImportance.Normal, "DeletePlistEntries Task");
			Log.LogMessage (MessageImportance.Normal, $"  Plist: {Plist}");
			Log.LogMessage (MessageImportance.Normal, $"  EntriesToDelete: {entriesString}");
			Log.LogMessage (MessageImportance.Normal, $"  IgnoreResult: {IgnoreResult}");

			var plistBuddyCommands = entriesToDelete.Select (entry => new PlistBuddyCommand {
				Verb = PlistBuddyVerb.Remove,
				VerbArguments = new [] { entry }
			});

			try {
				PlistBuddy.Run (Plist, plistBuddyCommands);
			} catch when (IgnoreResult) {
			}

			return true;
		}
	}
}
