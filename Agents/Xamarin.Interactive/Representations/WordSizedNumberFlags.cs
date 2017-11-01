//
// WordSizedNumberFlags.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Representations
{
	[Serializable]
	[Flags]
	enum WordSizedNumberFlags : byte
	{
		None = 0,
		Signed = 1,
		Real = 2,
		Pointer = 4
	}
}