//
// IEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;

namespace Xamarin.Interactive.Telemetry
{
	interface IEvent
	{
		string Key { get; }
		DateTime Timestamp { get; }
	}
}