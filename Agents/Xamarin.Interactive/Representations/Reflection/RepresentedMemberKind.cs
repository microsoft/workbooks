//
// RepresentedMemberKind.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Representations.Reflection
{
	[Serializable]
	public enum RepresentedMemberKind : byte
	{
		None,
		Field,
		Property
	}
}