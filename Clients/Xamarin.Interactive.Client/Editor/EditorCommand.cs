//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Editor
{
    struct EditorCommand
    {
        public string Id { get; }
        public string Title { get; }
        public string Tooltip { get; }

        public EditorCommand (string id, string title, string tooltip = null)
        {
            if (id == null)
                throw new ArgumentNullException (nameof (id));

            if (title == null)
                throw new ArgumentNullException (nameof (title));

            Id = id;
            Title = title;
            Tooltip = tooltip;
        }
    }
}