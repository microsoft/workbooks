//
// ReloadXcodeSettings.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Reflection;

using Microsoft.Build.Utilities;

namespace Xamarin.MSBuild
{
	public sealed class ReloadXcodeSettings : Task
	{
		public string XcodePath { get; set; }

		public override bool Execute ()
		{
			if (XcodePath == null)
				Log.LogMessage ($"Unsetting MD_APPLE_SDK_ROOT");
			else
				Log.LogMessage ($"Setting MD_APPLE_SDK_ROOT to {XcodePath}");

			Environment.SetEnvironmentVariable ("MD_APPLE_SDK_ROOT", XcodePath);

			var assembly = Assembly.Load ("Xamarin.MacDev");
			if (assembly == null) {
				Log.LogMessage ("Xamarin.MacDev.dll is not loaded in the app domain");
				return true;
			}

			var settingsType = assembly.GetType ("Xamarin.MacDev.AppleSdkSettings");
			if (settingsType == null) {
				Log.LogError ($"Cannot load Xamarin.MacDev.AppleSdkSettings from {assembly}");
				return false;
			}

			var initMethod = settingsType.GetMethod (
				"Init",
				BindingFlags.NonPublic | BindingFlags.Static);
			if (initMethod == null) {
				Log.LogError ($"Cannot load {settingsType.FullName}.Init() from {assembly}");
				return false;
			}

			Log.LogMessage ($"Invoking {initMethod.DeclaringType.FullName}.{initMethod.Name}");

			initMethod.Invoke (null, null);

			return true;
		}
	}
}