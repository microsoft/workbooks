﻿//
// SetPlistValue.cs
//
// Author:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
	public class SetPlistValue : Task
	{
		[Required]
		public string Plist { get; set; }

		[Required]
		public string Selector { get; set; }

		[Required]
		public string NewValue { get; set; }

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.Normal, "SetPlistValue Task");
			Log.LogMessage (MessageImportance.Normal, $"  Plist: {Plist}");
			Log.LogMessage (MessageImportance.Normal, $"  Selector: {Selector}");
			Log.LogMessage (MessageImportance.Normal, $"  NewValue: {NewValue}");

			PlistBuddy.Run (Plist,
				PlistBuddyVerb.Set,
				Selector,
				NewValue);

			return true;
		}
	}
}
