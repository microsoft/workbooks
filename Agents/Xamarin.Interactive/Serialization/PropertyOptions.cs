//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Serialization
{
    [Flags]
    public enum PropertyOptions
    {
        None = 0 << 0,
        SerializeIfNull = 1 << 0,
        SerializeIfEmpty = 1 << 1,

        Default = SerializeIfEmpty
    }
}