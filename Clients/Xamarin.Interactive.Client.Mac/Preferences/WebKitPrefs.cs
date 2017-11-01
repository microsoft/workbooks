//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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