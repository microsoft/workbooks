//
// KeyBinding.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Editor
{
	struct KeyBinding
	{
		readonly KeyModifier modifier;
		readonly string key;

		public KeyModifier Modifier => modifier;
		public string Key => key ?? String.Empty;

		public KeyBinding (string key, KeyModifier modifier = KeyModifier.None)
		{
			this.key = key;
			this.modifier = modifier;
		}

		public static KeyBinding Parse (string description)
		{
			if (String.IsNullOrEmpty (description))
				return default (KeyBinding);

			var modifier = KeyModifier.None;
			string key = null;

			foreach (var part in description.Split ('-')) {
				switch (part.ToLowerInvariant ()) {
				case "mod":
					modifier |= KeyModifier.Mod;
					break;
				case "alt":
					modifier |= KeyModifier.Alt;
					break;
				case "meta":
					modifier |= KeyModifier.Meta;
					break;
				case "ctrl":
				case "control":
					modifier |= KeyModifier.Ctrl;
					break;
				case "shift":
					modifier |= KeyModifier.Shift;
					break;
				default:
					key = part.Length == 1 ? part.ToLowerInvariant () : part;
					break;
				}
			}

			return new KeyBinding (key, modifier);
		}
	}
}