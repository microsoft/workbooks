//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Xamarin.Interactive.Preferences
{
    interface IPreferenceStore : ISimplyObservable<PreferenceChange>
    {
        void Set (string key, bool value);
        void Set (string key, long value);
        void Set (string key, double value);
        void Set (string key, string value);
        void Set (string key, string [] value);

        bool GetBoolean (string key, bool defaultValue = false);
        long GetInt64 (string key, long defaultValue = 0);
        double GetDouble (string key, double defaultValue = 0.0);
        string GetString (string key, string defaultValue = null);
        string [] GetStringArray (string key, string [] defaultValue = null);

        void Remove (string key);
        void RemoveAll ();

        ImmutableList<string> Keys { get; }
    }
}