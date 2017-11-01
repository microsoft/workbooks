//
// Build.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Xamarin.Interactive.Telemetry.Objects
{
	sealed class Build
	{
		public static Build Instance { get; } = new Build ();

		Build ()
		{
		}

		public void Serialize (JsonTextWriter writer)
		{
			writer.WriteStartObject ();

			writer.WritePropertyName ("version");
			writer.WriteValue (BuildInfo.VersionString);

			writer.WritePropertyName ("hash");
			writer.WriteValue (BuildInfo.Hash);

			writer.WriteEndObject ();
		}
	}
}