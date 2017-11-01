//
// KeyModifier.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Editor
{
	[Flags]
	enum KeyModifier
	{
		None = 0 << 0,
		Mod = 1 << 0,
		Meta = 1 << 1,
		Ctrl = 1 << 2,
		Alt = 1 << 3,
		Shift = 1 << 4
	}
}