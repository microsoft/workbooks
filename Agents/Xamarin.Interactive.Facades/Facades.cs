//
// Facades.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

namespace System
{
    [AttributeUsage (
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate,
        Inherited = false)]
    sealed class SerializableAttribute : Attribute
    {
    }
}