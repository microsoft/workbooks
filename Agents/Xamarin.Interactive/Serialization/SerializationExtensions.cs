//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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