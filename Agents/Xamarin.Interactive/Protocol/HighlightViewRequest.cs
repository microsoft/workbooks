// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Protocol
{
    [JsonObject]
    sealed class HighlightViewRequest : MainThreadRequest<InspectView>
    {
        public double X { get; }
        public double Y { get; }
        public bool Clear { get; }
        public string HierarchyKind { get; }

        [JsonConstructor]
        public HighlightViewRequest (double x, double y, bool clear, string hierarchyKind)
        {
            X = x;
            Y = y;
            Clear = clear;
            HierarchyKind = hierarchyKind;
        }

        protected override Task<InspectView> HandleAsync (Agent agent)
            => Task.FromResult (agent.HighlightView (X, Y, Clear, HierarchyKind));
    }
}