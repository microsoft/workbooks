//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Xamarin.Interactive
{
    static class EmptyArray<T>
    {
        public static readonly T [] Instance = new T [0];
    }
}