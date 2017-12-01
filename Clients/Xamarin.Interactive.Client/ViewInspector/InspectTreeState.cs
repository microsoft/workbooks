//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.ViewInspector
{
    class InspectTreeState
    {
        public int TotalCount { get; private set; }
        public DisplayMode Mode { get; }
        public bool ShowHidden { get; }

        List<int> ancestors = new List<int> { 0 };

        public InspectTreeState (DisplayMode mode, bool showHidden)
        {
            Mode = mode;
            ShowHidden = showHidden;
        }

        public void PushGeneration ()
        {
            ancestors.Add (0);
        }

        public int AddChild (InspectView view)
        {
            var pos = ancestors.Count - 1;
            var count = ancestors [pos];
            ancestors [pos] = count + 1;
            TotalCount = TotalCount + 1;

            return count;
        }

        public int PopGeneration ()
        {
            var pos = ancestors.Count - 1;
            var count = ancestors [pos];

            ancestors.RemoveAt (ancestors.Count - 1);
            return count;
        }
    }
}
