//
// DomApiAttributes.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.
//
// Analysis disable CheckNamespace

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