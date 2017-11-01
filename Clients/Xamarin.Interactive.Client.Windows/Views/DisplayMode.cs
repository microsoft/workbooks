// DisplayMode.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.
using System;

namespace Xamarin.Interactive.Client.Windows.Views
{
	[Flags]
	enum DisplayMode
	{
		None = 0,
		Frames = 1,
		Content = 1 << 1,
		FramesAndContent = Frames | Content
	}
}
