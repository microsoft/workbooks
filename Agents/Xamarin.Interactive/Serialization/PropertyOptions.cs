//
// PropertyOptions.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Serialization
{
	[Flags]
	public enum PropertyOptions
	{
		None = 0 << 0,
		SerializeIfNull = 1 << 0,
		SerializeIfEmpty = 1 << 1,

		Default = SerializeIfEmpty
	}
}