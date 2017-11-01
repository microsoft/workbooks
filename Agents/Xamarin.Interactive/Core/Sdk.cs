//
// Sdk.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace Xamarin.Interactive.Core
{
    [Serializable]
    sealed class Sdk : ISerializable
    {
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

        public Sdk (
            FrameworkName targetFramework,
            IEnumerable<string> assemblySearchPaths,
            string name = null,
            string profile = null,
            string version = null)
        {
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

        Sdk (SerializationInfo info, StreamingContext context)
        {
            var targetFramework = info.GetString (nameof (TargetFramework));
            if (targetFramework != null)
                TargetFramework = new FrameworkName (targetFramework);
            AssemblySearchPaths = (string [])info.GetValue (
                nameof (AssemblySearchPaths),
                typeof (string []));
            Name = info.GetString (nameof (Name));
            Profile = info.GetString (nameof (Profile));
            Version = info.GetString (nameof (Version));
        }

        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue (nameof (TargetFramework), TargetFramework.FullName);
            info.AddValue (nameof (AssemblySearchPaths), AssemblySearchPaths);
            info.AddValue (nameof (Name), Name);
            info.AddValue (nameof (Profile), Profile);
            info.AddValue (nameof (Version), Version);
        }

        public static Sdk FromEntryAssembly (
            string name = null,
            string profile = null,
            string version = null)
            => new Sdk (
                new FrameworkName (Assembly
                    .GetEntryAssembly ()
                    .GetCustomAttribute<TargetFrameworkAttribute> ()
                    .FrameworkName),
                Array.Empty<string> (),
                name,
                profile,
                version);
    }
}