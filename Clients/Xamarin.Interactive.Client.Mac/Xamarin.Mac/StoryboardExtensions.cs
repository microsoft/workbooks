//
// StoryboardExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014 Xamarin Inc. All rights reserved.

using Foundation;

namespace AppKit
{
    static class StoryboardExtensions
    {
        public static TController InstantiateController<TController> (
            this NSStoryboard storyboard) where TController : NSObject
            => (TController)storyboard.InstantiateControllerWithIdentifier (typeof (TController).Name);
    }
}