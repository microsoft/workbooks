//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

using Newtonsoft.Json;

namespace Xamarin.Interactive
{
    [JsonObject]
    public sealed class Sdk
    {
        /// <summary>
        /// A unique identifier for the SDK such as "console-dotnetcore".
        /// </summary>
        public SdkId Id { get; }

        /// <summary>
        /// A user-friendly display name for the SDK
        /// product such as ".NET Core" or "Xamarin.iOS".
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A profile to differentiate <see cref="Name"/> in the case
        /// where there may be more than one <see cref="Sdk"/>
        /// instance with the same <see cref="Name"/>. For example,
        /// Xamarin.Mac would specify "Modern" and "Full"
        /// here. May be <c>null</c>.
        /// </summary>
        public string Profile { get; }

        /// <summary>
        /// The version of the framework, such as "10.12.0.14"
        /// for Xamarin.iOS. Note that this is not the same as
        /// a Target Framework name or moniker for .NET/NuGet.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// The target framework name, such as ".NETFramework,Version=4.6.1".
        /// </summary>
        public FrameworkName TargetFramework { get; }

        /// <summary>
        /// Search paths for resolving framework assemblies.
        /// </summary>
        public IReadOnlyList<string> AssemblySearchPaths { get; }

        [JsonConstructor]
        public Sdk (
            SdkId id,
            FrameworkName targetFramework,
            IEnumerable<string> assemblySearchPaths,
            string name = null,
            string profile = null,
            string version = null)
        {
            Id = ((string)id)
                ?? throw new ArgumentNullException (nameof (id));

            TargetFramework = targetFramework
                ?? throw new ArgumentNullException (nameof (targetFramework));

            AssemblySearchPaths = assemblySearchPaths?.ToArray ()
                ?? throw new ArgumentNullException (nameof (assemblySearchPaths));

            Name = name;
            Profile = profile;
            Version = version;

            if (Name != null || Version != null)
                return;

            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                Name = "System Mono";
                const string monoVersionPath = "/Library/Frameworks/Mono.framework/Versions/Current/VERSION";
                if (File.Exists (monoVersionPath))
                    Version = File.ReadAllText (monoVersionPath)?.Trim ();
            } else {
                var versionInfo = FileVersionInfo.GetVersionInfo (typeof (object).Assembly.Location);
                Name = ".NET Framework";
                Version = versionInfo.ProductVersion;
            }
        }

        public static Sdk FromEntryAssembly (
            SdkId id,
            string name = null,
            string profile = null,
            string version = null)
            => new Sdk (
                id,
                new FrameworkName (Assembly
                    .GetEntryAssembly ()
                    .GetCustomAttribute<TargetFrameworkAttribute> ()
                    .FrameworkName),
                Array.Empty<string> (),
                name,
                profile,
                version);
    }

    public static class SdkExtensions
    {
        public static bool Is (this Sdk sdk, SdkId id)
        {
            if (id.IsNull)
                throw new ArgumentNullException (nameof (id));

            return sdk != null && sdk.Id == id;
        }

        public static bool IsNot (this Sdk sdk, SdkId id)
        {
            if (id.IsNull)
                throw new ArgumentNullException (nameof (id));

            return sdk == null || sdk.Id != id;
        }
    }
}