//
// BuildInfoAttribute.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

using System;

using Xamarin.Versioning;

[assembly: Xamarin.Interactive.BuildInfo]

namespace Xamarin.Interactive
{
	[AttributeUsage (AttributeTargets.Assembly)]
	public sealed class BuildInfoAttribute : Attribute
	{
		internal BuildInfoAttribute ()
		{
		}

		public DateTime Date => BuildInfo.Date;
		public string VersionString => BuildInfo.VersionString;
		internal ReleaseVersion Version => BuildInfo.Version;
		internal string Branch => BuildInfo.Branch;
		internal string Hash => BuildInfo.Hash;
		internal string HashShort => BuildInfo.HashShort;
		internal string BuildHostLane => BuildInfo.BuildHostLane;
	}
}