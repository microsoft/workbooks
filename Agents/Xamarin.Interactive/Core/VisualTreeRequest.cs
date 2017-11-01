//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class VisualTreeRequest : MainThreadRequest<InspectView>
    {
        public string HierarchyKind { get; }
        public bool CaptureViews { get; }

        protected override bool CanReturnNull => true;

        public VisualTreeRequest (string hierarchyKind, bool captureViews)
        {
            if (hierarchyKind == null)
                throw new ArgumentNullException (nameof (hierarchyKind));

            HierarchyKind = hierarchyKind;
            CaptureViews = captureViews;
        }

        protected override Task<InspectView> HandleAsync (Agent agent)
        {
            var result = agent.GetVisualTree (HierarchyKind);
            if (CaptureViews)
                result?.CaptureAll ();
            return Task.FromResult (result);
        }
    }
}