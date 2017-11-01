//
// DisplayMode.cs
//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace Xamarin.Interactive.Client.Mac.ViewInspector
{
	public enum DisplayMode
	{
		// These match to the tag values of the corersponding menu items in the
		// storyboard to make it easier to validate the menu items.
		Frames = 5,
		Content = 6,
		FramesAndContent = 7
	}
}
