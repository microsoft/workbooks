//
// PreferencesViewController.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

using AppKit;
using Foundation;

namespace Xamarin.Interactive.Preferences
{
	abstract class PreferencesViewController : NSViewController
	{
		ImmutableBidirectionalDictionary<Preference<bool>, NSButton> checkButtonPrefs
			= ImmutableBidirectionalDictionary<Preference<bool>, NSButton>.Empty;

		protected PreferencesViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			PreferenceStore.Default.Subscribe (ObservePreferenceChange);
		}

		protected virtual void ObservePreferenceChange (PreferenceChange change)
		{
			foreach (var binding in checkButtonPrefs.FirstToSecond) {
				if (binding.Key.Key == change.Key) {
					HandlePreferenceChange (binding.Key, binding.Value);
					return;
				}
			}
		}

		protected void AddCheckButtonPreference (Preference<bool> preference, NSButton checkButton)
		{
			checkButton.Activated += (sender, e) => {
				Preference<bool> pref;
				if (checkButtonPrefs.TryGetFirst (checkButton, out pref))
					pref.SetValue (checkButton.State == NSCellStateValue.On);
			};

			checkButtonPrefs = checkButtonPrefs.Add (preference, checkButton);
			HandlePreferenceChange (preference, checkButton);
		}

		void HandlePreferenceChange (Preference<bool> pref, NSButton checkButton)
			=> checkButton.State = pref.GetValue () ? NSCellStateValue.On : NSCellStateValue.Off;
	}
}