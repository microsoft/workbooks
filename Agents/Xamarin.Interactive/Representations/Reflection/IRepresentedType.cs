//
// IRepresentedType.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Representations.Reflection
{
	public interface IRepresentedType
	{
		string Name { get; }
		Type ResolvedType { get; }
		IRepresentedType BaseType { get; }
	}
}