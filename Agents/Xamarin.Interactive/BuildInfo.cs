//
// BuildInfo.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2014-2016 Xamarin Inc. All rights reserved.
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Reflection;

using Xamarin.Versioning;

namespace Xamarin.Interactive
{
    static class BuildInfo
    {
        public static readonly string Copyright = typeof (BuildInfo)
            .GetTypeInfo ()
            .Assembly
            ?.GetCustomAttribute<AssemblyCopyrightAttribute> ()
            ?.Copyright;

        public const string VersionString = "0.0.0.0";
        public const string Hash = "@PACKAGE_HEAD_REV@";
        public const string HashShort = "@PACKAGE_HEAD_REV_SHORT@";
        public const string Branch = "@PACKAGE_HEAD_BRANCH@";
        public const string BuildHostLane = "@PACKAGE_BUILD_HOST_LANE@";

        const string updateVersion = "@PACKAGE_UPDATEINFO_VERSION@";
        public static readonly long UpdateVersion =
            updateVersion [0] == '@' ? 0 : long.Parse (updateVersion);

        public static readonly ReleaseVersion Version;
        public static readonly DateTime Date;

        public static bool IsLocalDebugBuild;

        static BuildInfo ()
        {
            if (!ReleaseVersion.TryParse (VersionString, out Version))
                Version = new ReleaseVersion (0, 0, 0, ReleaseCandidateLevel.Local);

            DateTime.TryParse ("@PACKAGE_BUILD_DATE@", out Date);

            #if DEBUG
			IsLocalDebugBuild = Version.CandidateLevel == ReleaseCandidateLevel.Local;
            #else
            IsLocalDebugBuild = false;
            #endif
        }
    }
}