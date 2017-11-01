//
// IDataEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Xamarin.Interactive.Telemetry
{
	interface IDataEvent : IEvent
	{
		Task SerializePropertiesAsync (JsonTextWriter writer);
	}
}