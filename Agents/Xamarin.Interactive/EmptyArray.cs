//
// EmptyArray.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

namespace Xamarin.Interactive
{
    static class EmptyArray<T>
    {
        public static readonly T [] Instance = new T [0];
    }
}