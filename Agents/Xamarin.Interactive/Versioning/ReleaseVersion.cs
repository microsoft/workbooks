//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Xamarin.Versioning
{
    enum ReleaseCandidateLevel : byte
    {
        Local = 0,
        Dev = 1,
        Alpha = 3,
        Beta = 5,
        StableCandidate = 7,
        Stable = 9
    }

    enum ReleaseVersionFormat
    {
        /// <summary>
        /// A SemVer 2.0 compatible format.
        /// </summary>
        SemVer,

        /// <summary>
        /// Info.plist compatible CFBundleVersion.
        /// This format will drop the Build component since CFBundleVersion
        /// only allows for four components (and only three components for
        /// stable versions). The Major component must also be >= 1.
        /// </summary>
        AppleCFBundleVersion,

        /// <summary>
        /// Info.plist compatible CFBundleShortVersionString.
        /// This format will drop the Build component since CFBundleShortVersionString
        /// only allows for four components (and only three components for
        /// stable versions). The Major component must also be >= 1.
        /// </summary>
        AppleCFBundleShortVersion,

        /// <summary>
        /// major.minor.patch.candidate format that does not include the build
        /// component. The candidate component is computed based on the
        /// CandidateLevel and Candidate. This format is for compatibility with
        /// <see cref="System.Reflection.AssemblyFileVersionAttribute"/>, which
        /// is itself based on the limitations of file versioning in Windows
        /// where version numbers are [byte].[byte].[ushort].[ushort].
        /// </summary>
        /// <remarks>
        /// The last component uses a basic formula for computing a friendly
        /// decimal value that includes the CandidateLevel and the Candidate
        /// value: <code>(int)CandidateLevel * 1000 + Candidate</code>.
        /// For example:
        /// <para>(CandidateLevel: Stable, Candidate: 10) = 9010</para>
        /// <para>(CandidateLevel: Beta, Candidate: 3) = 5003</para>
        /// </remarks>
        WindowsFileVersion,

        /// <summary>
        /// A localized short string for use in release notes, documentation,
        /// etc. Examples: "1.2", "1.2 RC 4", "1.2.1 RC 3 Build 5".
        /// </summary>
        FriendlyShort,

        /// <summary>
        /// A localized long string for use in release notes, documentation,
        /// etc. Example: "1.2.1 Release Candidate 3". The only difference
        /// between <see cref="FriendlyShort"/> and <see cref="FriendlyLong"/>
        /// is that "RC" is expanded to "Release Candidate".
        /// </summary>
        FriendlyLong
    }

    [Serializable]
    struct ReleaseVersion : IComparable<ReleaseVersion>, IEquatable<ReleaseVersion>
    {
        /// <remarks>
        /// A stricter subset of semver, the pre-release and build components
        /// are required to be '(rc|beta|alpha|dev).?\d+' and '\d+' if they
        /// are present. Arbitrary strings for these components cannot map to our
        /// updateinfo integer format.
        /// </remarks>
        static readonly Regex semverRegex = new Regex (
            @"^(?<major>\d{1,2})" +
            @"(\.(?<minor>\d{1,2}))" +
            @"(\.(?<patch>\d{1,2}))" +
            @"(\-(?<level>(local|dev|alpha|beta|rc))(\.?(?<candidate>\d{1,2}))?)?" +
            @"(\+(build\.)?(?<build>\d{1,4}))?$",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        static bool TryParseGroup (Group group, out int match)
        {
            if (group != null && group.Success && int.TryParse (group.Value, out match))
                return true;
            match = 0;
            return false;
        }

        public static bool TryParse (string version, out ReleaseVersion releaseVersion)
        {
            Exception error;
            return TryParse (version, out releaseVersion, out error);
        }

        static bool TryParse (string version, out ReleaseVersion releaseVersion, out Exception error)
        {
            error = null;
            releaseVersion = default (ReleaseVersion);

            try {
                int major;
                int minor;
                int patch;
                ReleaseCandidateLevel level;
                int candidate;
                int build;

                var match = semverRegex.Match (version);
                if (match == null || !match.Success ||
                    !TryParseGroup (match.Groups ["major"], out major) ||
                    !TryParseGroup (match.Groups ["minor"], out minor) ||
                    !TryParseGroup (match.Groups ["patch"], out patch))
                    return false;

                TryParseGroup (match.Groups ["candidate"], out candidate);
                TryParseGroup (match.Groups ["build"], out build);

                level = ReleaseCandidateLevel.Stable;
                var levelMatch = match.Groups ["level"];
                if (levelMatch != null && levelMatch.Success) {
                    switch (levelMatch.Value.ToLowerInvariant ()) {
                    case "local":
                        level = ReleaseCandidateLevel.Local;
                        break;
                    case "dev":
                        level = ReleaseCandidateLevel.Dev;
                        break;
                    case "alpha":
                        level = ReleaseCandidateLevel.Alpha;
                        break;
                    case "beta":
                        level = ReleaseCandidateLevel.Beta;
                        break;
                    case "rc":
                        level = ReleaseCandidateLevel.StableCandidate;
                        break;
                    }
                }

                releaseVersion = new ReleaseVersion (
                    major,
                    minor,
                    patch,
                    level,
                    candidate,
                    build);
            } catch (Exception e) {
                error = e;
                releaseVersion = default (ReleaseVersion);
                return false;
            }

            return true;
        }

        public static ReleaseVersion Parse (string version)
        {
            ReleaseVersion releaseVersion;
            Exception error;
            if (!TryParse (version, out releaseVersion, out error))
                throw new FormatException ("must be in semver format", error);
            return releaseVersion;
        }

        public byte Major { get; }
        public byte Minor { get; }
        public byte Patch { get; }
        public ReleaseCandidateLevel CandidateLevel { get; }
        public byte Candidate { get; }
        public ushort Build { get; }

        public bool IsValid => Major > 0 || Minor > 0 || Patch > 0;
        public bool IsStableRelease => CandidateLevel >= ReleaseCandidateLevel.Stable;

        public ReleaseVersion (
            int major,
            int minor,
            int patch,
            ReleaseCandidateLevel candidateLevel = ReleaseCandidateLevel.Stable,
            int candidate = 0,
            int build = 0)
        {
            if (major < 0 || major > 99)
                throw new ArgumentOutOfRangeException (nameof (major), "must be 0 <= X <= 99");

            if (minor < 0 || minor > 99)
                throw new ArgumentOutOfRangeException (nameof (minor), "must be 0 <= X <= 99");

            if (patch < 0 || patch > 99)
                throw new ArgumentOutOfRangeException (nameof (patch), "must be 0 <= X <= 99");

            if (candidate < 0 || candidate > 99)
                throw new ArgumentOutOfRangeException (nameof (candidate), "must be 0 <= X <= 99");

            if (build < 0 || build > 9999)
                throw new ArgumentOutOfRangeException (nameof (build), "must be 0 <= X <= 9999");

            switch (candidateLevel) {
            case ReleaseCandidateLevel.Local:
            case ReleaseCandidateLevel.Dev:
            case ReleaseCandidateLevel.Alpha:
            case ReleaseCandidateLevel.Beta:
            case ReleaseCandidateLevel.StableCandidate:
                break;
            case ReleaseCandidateLevel.Stable:
                if (candidate != 0)
                    throw new ArgumentOutOfRangeException (
                        nameof (candidate),
                        $"must be 0 if {nameof (candidateLevel)} is Stable");
                break;
            default:
                throw new ArgumentOutOfRangeException (nameof (candidateLevel));
            }

            Major = (byte)major;
            Minor = (byte)minor;
            Patch = (byte)patch;
            CandidateLevel = candidateLevel;
            Candidate = (byte)candidate;
            Build = (ushort)build;
        }

        public ReleaseVersion WithBuild (int build)
        {
            if (Build == build)
                return this;

            return new ReleaseVersion (Major, Minor, Patch, CandidateLevel, Candidate, build);
        }

        public ReleaseVersion WithCandidateLevel (ReleaseCandidateLevel candidateLevel)
        {
            if (CandidateLevel == candidateLevel)
                return this;

            return new ReleaseVersion (Major, Minor, Patch, candidateLevel, Candidate, Build);
        }

        public override string ToString ()
            => ToSemVerString (true);

        public string ToString (
            ReleaseVersionFormat format,
            bool withBuildComponent = false,
            bool nugetSafeBuild = false)
        {
            switch (format) {
            case ReleaseVersionFormat.SemVer:
                return ToSemVerString (withBuildComponent, nugetSafeBuild);
            case ReleaseVersionFormat.AppleCFBundleVersion:
                return ToAppleCFBundleVersion ();
            case ReleaseVersionFormat.AppleCFBundleShortVersion:
                return ToAppleCFBundleShortVersion ();
            case ReleaseVersionFormat.WindowsFileVersion:
                return ToWindowsFileVersion ().ToString ();
            case ReleaseVersionFormat.FriendlyShort:
                return ToFriendlyString (true, withBuildComponent);
            case ReleaseVersionFormat.FriendlyLong:
                return ToFriendlyString (false, withBuildComponent);
            }

            throw new ArgumentOutOfRangeException (nameof (format));
        }

        string ToSemVerString (bool withBuild, bool nugetSafeBuild = false)
        {
            var builder = new StringBuilder (32);
            builder.AppendFormat (CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch);

            switch (CandidateLevel) {
            case ReleaseCandidateLevel.Local:
                builder.Append ("-local");
                break;
            case ReleaseCandidateLevel.Dev:
                builder.Append ("-dev");
                break;
            case ReleaseCandidateLevel.Alpha:
                builder.Append ("-alpha");
                break;
            case ReleaseCandidateLevel.Beta:
                builder.Append ("-beta");
                break;
            case ReleaseCandidateLevel.StableCandidate:
                builder.Append ("-rc");
                break;
            case ReleaseCandidateLevel.Stable:
                break;
            default:
                throw new InvalidOperationException ("should not be reached");
            }

            if (Candidate > 0)
                builder.AppendFormat (CultureInfo.InvariantCulture, "{0}", Candidate);

            if (Build > 0 && withBuild)
                builder.AppendFormat (
                    CultureInfo.InvariantCulture,
                    nugetSafeBuild ? "-build.{0}" : "+{0}",
                    Build);

            return builder.ToString ();
        }

        StringBuilder ToMajorMinorPatchOptionals (bool minorOptional, bool patchOptional)
        {
            var builder = new StringBuilder ();

            builder.AppendFormat (CultureInfo.InvariantCulture, "{0}", Major);

            if (!minorOptional || Minor > 0 || Patch > 0)
                builder.AppendFormat (CultureInfo.InvariantCulture, ".{0}", Minor);

            if (!patchOptional || Patch > 0)
                builder.AppendFormat (CultureInfo.InvariantCulture, ".{0}", Patch);

            return builder;
        }

        string ToAppleCFBundleVersion ()
        {
            if (Major == 0)
                throw new FormatException ("Major must be > 0 (CFBundleVersion requirements)");

            var builder = ToMajorMinorPatchOptionals (
                minorOptional: false,
                patchOptional: true);

            ushort candidate = Candidate;

            switch (CandidateLevel) {
            case ReleaseCandidateLevel.Local:
            case ReleaseCandidateLevel.Dev:
                builder.Append ('d');
                candidate = Build;
                break;
            case ReleaseCandidateLevel.Alpha:
                builder.Append ('a');
                break;
            case ReleaseCandidateLevel.Beta:
                builder.Append ('b');
                break;
            case ReleaseCandidateLevel.StableCandidate:
                builder.Append ("fc");
                break;
            default:
                return builder.ToString ();
            }

            builder.AppendFormat (CultureInfo.InvariantCulture, "{0}", candidate);

            return builder.ToString ();
        }

        string ToAppleCFBundleShortVersion ()
        {
            if (Major == 0)
                throw new FormatException ("Major must be > 0 (CFBundleVersion requirements)");

            return ToMajorMinorPatchOptionals (
                minorOptional: false,
                patchOptional: true).ToString ();
        }

        string ToFriendlyString (bool @short, bool withBuild)
        {
            var builder = ToMajorMinorPatchOptionals (
                minorOptional: true,
                patchOptional: true);

            // FIXME: localize
            switch (CandidateLevel) {
            case ReleaseCandidateLevel.Local:
                builder.Append (" Local");
                break;
            case ReleaseCandidateLevel.Dev:
                builder.Append (" Development");
                break;
            case ReleaseCandidateLevel.Alpha:
                builder.Append (" Alpha");
                break;
            case ReleaseCandidateLevel.Beta:
                builder.Append (" Beta");
                break;
            case ReleaseCandidateLevel.StableCandidate:
                if (@short)
                    builder.Append (" RC");
                else
                    builder.Append (" Release Candidate");
                break;
            case ReleaseCandidateLevel.Stable:
                break;
            default:
                throw new InvalidOperationException ("should not be reached");
            }

            if (Candidate > 0)
                builder.AppendFormat (CultureInfo.InvariantCulture, " {0}", Candidate);

            if (Build > 0 && withBuild)
                builder.AppendFormat (CultureInfo.InvariantCulture, " Build {0}", Build);

            return builder.ToString ();
        }

        public Version ToWindowsFileVersion ()
        {
            // System.Version only has four numeric components whereas SemVer has 5 components.
            // We drop the build component here, which realistically should always be 0 for
            // public releases. To ensure that stable releases are always with a higher version
            // than candidate releases, the candidate level is multiplied by 1000, and then
            // the candidate number added to it. This keeps the last component legible in base
            // 10 and allows for ~2000 candidates within a level, which will not be reached
            // by this data structure, which only allows for 255.
            return new Version (Major, Minor, Patch, (int)CandidateLevel * 1000 + Candidate);
        }

        public static ReleaseVersion FromWindowsFileVersion (Version version)
        {
            return new ReleaseVersion (
                version.Major,
                version.Minor,
                version.Build,
                (ReleaseCandidateLevel)(version.Revision / 1000),
                version.Revision % 1000);
        }

        public int CompareTo (ReleaseVersion other)
        {
            if (Major != other.Major)
                return Major > other.Major ? 1 : -1;

            if (Minor != other.Minor)
                return Minor > other.Minor ? 1 : -1;

            if (Patch != other.Patch)
                return Patch > other.Patch ? 1 : -1;

            if (CandidateLevel != other.CandidateLevel)
                return CandidateLevel > other.CandidateLevel ? 1 : -1;

            if (Candidate != other.Candidate)
                return Candidate > other.Candidate ? 1 : -1;

            if (Build != other.Build)
                return Build > other.Build ? 1 : -1;

            return 0;
        }

        public bool Equals (ReleaseVersion other)
            => other.Major == Major &&
                        other.Minor == Minor &&
                        other.Patch == Patch &&
                        other.CandidateLevel == CandidateLevel &&
                        other.Candidate == Candidate &&
                        other.Build == Build;

        public override bool Equals (object obj)
            => obj is ReleaseVersion && Equals ((ReleaseVersion)obj);

        public override int GetHashCode ()
        {
            // unrolled from Xamarin.Interactive.Hash.Combine to not take a dep
            const int factor = unchecked((int)0xa5555529);
            var hash = 1;
            hash = unchecked(hash * factor + Major);
            hash = unchecked(hash * factor + Minor);
            hash = unchecked(hash * factor + Patch);
            hash = unchecked(hash * factor + (byte)CandidateLevel);
            hash = unchecked(hash * factor + Candidate);
            hash = unchecked(hash * factor + Build);
            return hash;
        }

        public static bool operator == (ReleaseVersion x, ReleaseVersion y)
            => x.Equals (y);

        public static bool operator != (ReleaseVersion x, ReleaseVersion y)
            => !x.Equals (y);

        public static bool operator < (ReleaseVersion x, ReleaseVersion y)
            => x.CompareTo (y) < 0;

        public static bool operator > (ReleaseVersion x, ReleaseVersion y)
            => x.CompareTo (y) > 0;

        public static bool operator >= (ReleaseVersion x, ReleaseVersion y)
            => x.CompareTo (y) >= 0;

        public static bool operator <= (ReleaseVersion x, ReleaseVersion y)
            => x.CompareTo (y) <= 0;
    }
}