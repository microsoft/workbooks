//
// PreferenceChange.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Preferences
{
	struct PreferenceChange
	{
		public string Key { get; }

		internal PreferenceChange (string key)
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));

			Key = key;
		}
	}
}