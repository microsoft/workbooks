//
// WebKitPrefs.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using WebKit;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Preferences
{
	static class WebKitPrefs
	{
		const string TAG = nameof (WebKitPrefs);

		public static bool DeveloperExtrasEnabled {
			get {
				using (NativeExceptionHandler.Trap ()) {
					try {
						return WebPreferencesPrivate.GetDeveloperExtrasEnabled (
							WebPreferences.StandardPreferences);
					} catch (Exception e) {
						Log.Error (TAG, "private WebKit API may have been removed", e);
						return false;
					}
				}
			}
		}
	}
}