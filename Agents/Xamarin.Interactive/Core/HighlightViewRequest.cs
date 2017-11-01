//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class HighlightViewRequest : MainThreadRequest<InspectView>
    {
        public double X { get; set; }
        public double Y { get; set; }
        public bool Clear { get; set; }
        public string HierarchyKind { get; set; }

        protected override Task<InspectView> HandleAsync (Agent agent)
        {
            return Task.FromResult (agent.HighlightView (X, Y, Clear, HierarchyKind));
        }
    }
}