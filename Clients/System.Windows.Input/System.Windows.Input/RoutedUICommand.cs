//
// RoutedUICommand.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

namespace System.Windows.Input
{
	public class RoutedUICommand : RoutedCommand
	{
		public string Text { get; }

		public RoutedUICommand (
			string text,
			string name,
			Type ownerType,
			InputGestureCollection inputGestures = null)
			: base (name, ownerType, inputGestures)
		{
			Text = text;
		}
	}
}