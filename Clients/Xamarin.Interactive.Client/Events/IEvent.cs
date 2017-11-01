//
// IEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;

namespace Xamarin.Interactive.Events
{
	public interface IEvent
	{
		object Source { get; }
		DateTime Timestamp { get; }
	}
}