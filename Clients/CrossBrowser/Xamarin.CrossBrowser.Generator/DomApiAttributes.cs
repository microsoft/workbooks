//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

enum Backend
{
    JavaScriptCore,
    Mshtml
}

class TypeAttribute : Attribute
{
    public Backend Backend { get; private set; }
    public string TypeName { get; private set; }
    public string BaseTypeName { get; private set; }

    public TypeAttribute (Backend backend, string typeName, string baseTypeName = null)
    {
        Backend = backend;
        TypeName = typeName;
        BaseTypeName = baseTypeName;
    }
}

class IgnoreAttribute : Attribute
{
    public Backend Backend { get; private set; }

    public IgnoreAttribute (Backend backend)
    {
        Backend = backend;
    }
}

class IReadOnlyListAttribute : Attribute
{
}

class MshtmlConvertAttribute : Attribute
{
}