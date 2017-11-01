//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Rendering
{
    [Flags]
    public enum RendererRepresentationOptions
    {
        None = 0,

        /// <summary>
        /// The representation will always be provided the expanded render
        /// targets and will not be collapsible at all.
        /// </summary>
        ForceExpand = 1,

        /// <summary>
        /// The representation is collapsible, but will be expanded by default.
        /// </summary>
        ExpandedByDefault = 2,

        /// <summary>
        /// The representation is collapsible and will be collapsed by default if
        /// it is the only or initially selected renderer, and otherwise expanded
        /// automatically when selected from the menu.
        /// </summary>
        ExpandedFromMenu = 4,

        /// <summary>
        /// The display name of the representation will be suppressed in the 
        /// representation button label and shown only in the button's menu,
        /// but only if all other representations also have the hint.
        /// </summary>
        SuppressDisplayNameHint = 8
    }
}