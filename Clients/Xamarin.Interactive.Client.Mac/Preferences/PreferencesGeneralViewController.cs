//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;
using Foundation;

using Xamarin.Interactive.Client;

namespace Xamarin.Interactive.Preferences
{
    sealed partial class PreferencesGeneralViewController : PreferencesViewController
    {
        PreferencesGeneralViewController (IntPtr handle) : base (handle)
        {
        }

        [Export ("fontSize")]
        nfloat FontSize {
            get { return (nfloat)Prefs.UI.Font.GetSize (); }
            set { Prefs.UI.Font.Update (UIFontPreference.UpdateAction.Set, value); }
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            if (ClientInfo.Flavor != ClientFlavor.Inspector)
                saveHistoryCheckButton.Hidden = true;

            fontSizeStepper.MinValue = Prefs.UI.Font.MinFontSize;
            fontSizeStepper.MaxValue = Prefs.UI.Font.MaxFontSize;
            var fontSizeFormatter = (NSNumberFormatter)fontSizeTextField.Formatter;
            fontSizeFormatter.Minimum = NSNumber.FromDouble (Prefs.UI.Font.MinFontSize);
            fontSizeFormatter.Maximum = NSNumber.FromDouble (Prefs.UI.Font.MaxFontSize);

            fontSizeTextField.FocusRingType = NSFocusRingType.None;

            UpdateFont ();

            AddCheckButtonPreference (
                Prefs.Editor.ShowLineNumbers,
                showLineNumbersCheckButton);

            AddCheckButtonPreference (
                Prefs.Submissions.ShowExecutionTimings,
                showExecutionTimingsCheckButton);

            AddCheckButtonPreference (
                Prefs.Repl.SaveHistory,
                saveHistoryCheckButton);

            AddCheckButtonPreference (
                Prefs.Submissions.WrapLongLinesInEditor,
                wrapLongLinesInEditorCheckButton);
        }

        protected override void ObservePreferenceChange (PreferenceChange change)
        {
            base.ObservePreferenceChange (change);

            if (change.Key == Prefs.UI.Font.Key)
                UpdateFont ();
        }

        void UpdateFont ()
        {
            var fontSize = (nfloat)Prefs.UI.Font.GetSize ();
            fontSizeTextField.DoubleValue = fontSize;
            fontSizeStepper.DoubleValue = fontSize;
        }

        partial void ResetAllPreferences (NSObject sender)
            => PreferenceStore.Default.RemoveAll ();
    }
}