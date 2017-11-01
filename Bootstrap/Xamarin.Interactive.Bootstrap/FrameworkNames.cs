//
// FrameworkNames.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.

using System;
using System.Runtime.Versioning;

namespace Xamarin.Interactive.Core
{
	static class FrameworkNames
	{
		public static readonly FrameworkName Xamarin_iOS_1_0
			= new FrameworkName ("Xamarin.iOS", new Version (1, 0));

		public static readonly FrameworkName Xamarin_Mac_2_0
			= new FrameworkName ("Xamarin.Mac", new Version (2, 0));

		public static readonly FrameworkName MonoAndroid_5_0
			= new FrameworkName ("MonoAndroid", new Version (5, 0));

		public static readonly FrameworkName Net_4_5
			= new FrameworkName (".NETFramework", new Version (4, 5));

		public static readonly FrameworkName Net_4_6_1
			= new FrameworkName (".NETFramework", new Version (4, 6, 1));

		public static readonly FrameworkName DotNetCore2_0
			= new FrameworkName ("netcoreapp", new Version (2, 0, 0));
	}
}