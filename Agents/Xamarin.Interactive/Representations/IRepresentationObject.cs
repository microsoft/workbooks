//
// IRepresentationObject.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    interface IRepresentationObject : ISerializableObject
    {
    }

    // FIXME: remove when we IRepresentationObject is removed,
    // and everything is ported to ISerializableObject
    interface IFallbackRepresentationObject
    {
    }
}