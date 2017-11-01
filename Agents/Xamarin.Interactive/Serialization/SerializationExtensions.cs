//
// SerializationExtensions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System.Runtime.Serialization;

namespace Xamarin.Interactive.Serialization
{
    static class SerializationExtensions
    {
        public static T GetValue<T> (this SerializationInfo info, string name)
        {
            return (T)info.GetValue (name, typeof(T));
        }
    }
}