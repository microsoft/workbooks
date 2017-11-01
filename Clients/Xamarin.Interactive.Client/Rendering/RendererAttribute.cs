//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Rendering
{
    [AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RendererAttribute : Attribute
    {
        public Type SourceType { get; }
        public bool ExactMatchRequired { get; }

        public RendererAttribute (Type sourceType, bool exactMatchRequired = true)
        {
            if (sourceType == null)
                throw new ArgumentNullException (nameof(sourceType));

            SourceType = sourceType;
            ExactMatchRequired = exactMatchRequired;
        }
    }
}