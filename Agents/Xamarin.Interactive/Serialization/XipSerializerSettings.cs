//
// XipSerializerSettings.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System.Runtime.Serialization;

namespace Xamarin.Interactive.Serialization
{
    class XipSerializerSettings
    {
        public SerializationBinder Binder { get; set; }
    }
}