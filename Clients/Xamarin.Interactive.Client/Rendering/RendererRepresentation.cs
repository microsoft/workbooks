//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Rendering
{
    class RendererRepresentation
    {
        public string ShortDisplayName { get; }
        public object State { get; }
        public RendererRepresentationOptions Options { get; }
        public int Order { get; }

        public RendererRepresentation (
            string shortDisplayName,
            object state = null,
            RendererRepresentationOptions options = RendererRepresentationOptions.None,
            int order = 0)
        {
            if (shortDisplayName == null)
                throw new ArgumentNullException (nameof (shortDisplayName));

            ShortDisplayName = shortDisplayName;
            State = state;
            Options = options;
            Order = order;
        }

        public override string ToString () => ShortDisplayName;
    }
}