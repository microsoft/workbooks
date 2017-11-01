//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Preferences
{
    sealed class UIFontPreference
    {
        public enum UpdateAction
        {
            ResetDefault,
            Set,
            Increase,
            Decrease
        }

        Preference<double> preference;

        public double MinFontSize { get; set; } = 9;
        public double MaxFontSize { get; set; } = 72;

        public double DefaultFontSize
        {
            get { return preference.DefaultValue; }
            set { preference = new Preference<double> (preference.Key, value); }
        }

        public UIFontPreference ()
        {
             preference = new Preference<double> ("ui.font.size", 15);
        }

        public string Key => preference.Key;

        public double GetSize () => preference.GetValue ();

        public void Update (UpdateAction action, double? fontSize = null)
        {
            var previousFontSize = preference.GetValue ();
            var currentFontSize = previousFontSize;

            switch (action) {
            case UpdateAction.ResetDefault:
                preference.Reset ();
                currentFontSize = preference.GetValue ();
                break;
            case UpdateAction.Increase:
                currentFontSize = Math.Ceiling (currentFontSize) + 1;
                break;
            case UpdateAction.Decrease:
                currentFontSize = Math.Floor (currentFontSize) - 1;
                break;
            case UpdateAction.Set:
                if (fontSize == null)
                    throw new ArgumentNullException (
                        nameof (fontSize),
                        $"UpdateAction.Set requires the {nameof (fontSize)} argument");
                currentFontSize = fontSize.Value;
                break;
            }

            currentFontSize = Math.Max (MinFontSize, Math.Min (currentFontSize, MaxFontSize));

            if (currentFontSize == previousFontSize)
                return;

            preference.SetValue (currentFontSize);
        }
    }
}