//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    partial class NewWorkbookCollectionView : NSCollectionView
    {
        NewWorkbookCollectionView (IntPtr handle) : base (handle)
        {
        }

        public override void KeyDown (NSEvent theEvent)
        {
            switch (theEvent.KeyCode) {
            case 36:
            case 76:
                Superview.KeyDown (theEvent);
                break;
            default:
                base.KeyDown (theEvent);
                break;
            }
        }
    }
}