//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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