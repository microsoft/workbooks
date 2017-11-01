//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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