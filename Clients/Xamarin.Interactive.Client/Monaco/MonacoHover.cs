//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.CodeAnalysis.Hover;

namespace Xamarin.Interactive.Client.Monaco
{
    class MonacoHover
    {
        public string [] Contents { get; }

        public MonacoRange Range { get; }

        public MonacoHover (HoverViewModel hover)
        {
            Contents = hover.Contents;
            Range = new MonacoRange (hover.Range);
        }
    }
}
