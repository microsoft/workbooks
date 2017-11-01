//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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