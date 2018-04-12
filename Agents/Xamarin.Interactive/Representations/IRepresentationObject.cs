//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    public interface IRepresentationObject : ISerializableObject
    {
    }

    // FIXME: remove when we IRepresentationObject is removed,
    // and everything is ported to ISerializableObject
    interface IFallbackRepresentationObject
    {
    }
}