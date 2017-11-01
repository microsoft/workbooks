//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Representations.Reflection
{
    [Serializable]
    public enum RepresentedMemberKind : byte
    {
        None,
        Field,
        Property
    }
}