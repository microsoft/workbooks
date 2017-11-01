//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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