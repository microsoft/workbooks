//
// SoftwareComponent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using Newtonsoft.Json;

namespace Xamarin.Interactive.SystemInformation
{
	interface ISoftwareComponent
	{
		string Name { get; }
		string Version { get; }
		bool IsInstalled { get; }

		void SerializeExtraProperties (JsonTextWriter writer);
	}
}